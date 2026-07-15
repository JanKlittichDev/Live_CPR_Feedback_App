using System.Diagnostics;
using Live_CPR_Feedback_App.Models;
using MathNet.Filtering;
using MathNet.Filtering.IIR;

namespace Live_CPR_Feedback_App.Services
{
    // SIGNGALVERABEITUNGS-PIPELINE:
    //
    // Beschleunigungsbetrag
    // -> Offset-Entfernung
    // -> Lowpass
    // -> 1. Integration (Geschwindigkeit)
    // -> Highpass
    // -> 2. Integration (Weg)
    // -> Highpass
    // -> ZigZag-Peak-Detection
    // -> Tiefe (Peak-to-Trough) + Frequenz (Maxima-Abstand)
    //
    // Feuert im festen Takt (EmitIntervalSec) ein AnalysisResult, damit auch NoFlow
    // (CompressionDetected == false) zuverlaessig gemeldet wird.
    public class AnalysisServiceSP : IAnalysisService
    {
        public event EventHandler<AnalysisResult>? AnalysisCompleted;

        // Leitlinien-Schwellen
        private const double DepthOptimalMinCm = 5.0;
        private const double DepthOptimalMaxCm = 6.0;
        private const double RateOptimalMinCpm = 100.0;
        private const double RateOptimalMaxCpm = 120.0;

        // Gewaehlte Filter-Grenzfrequenzen
        private const double LowpassCutoffHz = 5.5;   // Filter 1 (Beschleunigung)
        private const double HighpassCutoffHz = 0.5;  // Filter 2 & 3 (Geschwindigkeit, Weg) -> Drift entfernen

        // Peak-Detection (Werte in cm, da Trajektorie *100)
        private const double ProminenceCm = 1.0;              // noetige Umkehr, um ein Extremum zu bestaetigen
        private const double MinCompressionAmplitudeCm = 2.0; // wenn Peak-to-Trough kleiner -> ermittelte Tiefe als Rauschen verworfen
        private const double MinCompressionPeriodSec = 0.30;  // 0.3 entspricht 200 CPM -> unmoeglich -> Maximum ignorieren
        private const double RateWindowSec = 8.0;             // gleitendes Fenster fuer CPM
        private const double DepthWindowSec = 8.0;            // gleitendes Fenster fuer Tiefe
        private const double NoFlowTimeoutSec = 1.5;          // so lange keine Kompression -> NoFlow

        // Emissions alle 1 Sekunden
        private const double EmitIntervalSec = 1.0;
        private double _lastEmitSec;

        // Zeit
        private readonly Stopwatch _clock = new();
        private double _prevSampleSec;
        private bool _hasPrevSample;
        private double _lastCompressionTime = double.NegativeInfinity;

        // Abtastraten-Schaetzung 
        private const int SampleRateWindow = 5;
        private readonly double[] _dtBuffer = new double[SampleRateWindow];
        private int _dtIndex;
        private int _dtCount;
        private double _fs;

        // Filter + Integratoren 
        private readonly ButterworthFilter _lp = new();
        private readonly ButterworthFilter _hp1 = new();
        private readonly ButterworthFilter _hp2 = new();
        private readonly TrapezoidIntegrator _int1 = new();
        private readonly TrapezoidIntegrator _int2 = new();

        // Offset (DC/Gravitation) 
        private const int OffsetWindow = 5;
        private readonly double[] _offsetBuffer = new double[OffsetWindow];
        private int _offsetIndex;
        private int _offsetCount;

        // ZigZag-Extremum-Detektor 
        private bool _zigInit;
        private bool _lookingForMax = true;
        private double _extremeVal;
        private double _extremeTime;
        private bool _hasLastExtreme;
        private double _lastExtremeVal;

        // Rate/Tiefe-Fenster 
        private bool _hasLastMaxTime;
        private double _lastMaxTime;
        private readonly Queue<double> _maxTimes = new();                 // Zeitpunkte gueltiger Maxima
        private readonly Queue<(double time, double amp)> _depthSamples = new();

        



        // Algorithmus  ------------------------------------------------------------------------------------------------------------------------------------------------
        public void AddSample(AccSensorData sample)
        {
            if (!_clock.IsRunning) _clock.Start();
            double now = _clock.Elapsed.TotalSeconds;

            if (!_hasPrevSample)
            {
                _hasPrevSample = true;
                _prevSampleSec = now;
                return; // ohne dt keine Integration/Filterung moeglich -> ersten Sample nur als Seed nutzen
            }

            double dt = now - _prevSampleSec;
            if (dt <= 0) return; // doppelter Zeitstempel -> ueberspringen
            _prevSampleSec = now;

            UpdateSamplingRate(dt); // Filterkoeffizienten ggf. neu berechnen, BEVOR gefiltert wird

            double accRaw = Math.Sqrt(sample.X * sample.X + sample.Y * sample.Y + sample.Z * sample.Z);
            double offset = ComputeOffset(accRaw);
            double accCleared = (accRaw - offset) * 9.81; // *g & DC entfernt

            double accFiltered = _lp.Process(accCleared);
            double velo = _int1.Integrate(accFiltered, dt);
            double veloFiltered = _hp1.Process(velo);
            double pos = _int2.Integrate(veloFiltered, dt);
            double posFiltered = _hp2.Process(pos);
            double trajectoryCm = posFiltered * 100.0; // m -> cm

            ProcessZigZag(trajectoryCm, now);
            MaybeEmit(now);
        }


        // Abtastrate: nur ueber gefuellte Eintraege mitteln -----------------------------------------------------------------------------------------------------------------------------------
        private void UpdateSamplingRate(double dt)
        {
            _dtBuffer[_dtIndex] = dt;
            _dtIndex = (_dtIndex + 1) % _dtBuffer.Length;
            if (_dtCount < _dtBuffer.Length) _dtCount++;

            double sum = 0;
            for (int i = 0; i < _dtCount; i++) sum += _dtBuffer[i];
            double averageDt = sum / _dtCount;
            if (averageDt <= 0) return;

            double fsNew = 1.0 / averageDt;
            if (fsNew < 10.0 || fsNew > 500.0) return; // Sanity-Bounds

            if (Math.Abs(fsNew - _fs) > 1.0)
            {
                _fs = fsNew;
                _lp.SetLowpass(_fs, LowpassCutoffHz);
                _hp1.SetHighpass(_fs, HighpassCutoffHz);
                _hp2.SetHighpass(_fs, HighpassCutoffHz);
            }
        }

        private double ComputeOffset(double accRaw)
        {
            _offsetBuffer[_offsetIndex] = accRaw;
            _offsetIndex = (_offsetIndex + 1) % _offsetBuffer.Length;
            if (_offsetCount < _offsetBuffer.Length) _offsetCount++;

            double sum = 0;
            for (int i = 0; i < _offsetCount; i++) sum += _offsetBuffer[i];
            return sum / _offsetCount;
        }


        // ZigZag: Extrema wechseln sich zwangslaeufig ab --------------------------------------------------------------------------------------------------------------------------------------------
        private void ProcessZigZag(double value, double time)
        {
            if (!_zigInit)
            {
                _zigInit = true;
                _extremeVal = value;
                _extremeTime = time;
                _lookingForMax = true;
                return;
            }

            if (_lookingForMax)
            {
                if (value > _extremeVal) { _extremeVal = value; _extremeTime = time; }      // hoeheres Maximum
                else if (_extremeVal - value >= ProminenceCm)                                // Umkehr bestaetigt Max
                {
                    OnExtremeConfirmed(isMax: true, _extremeTime, _extremeVal);
                    _extremeVal = value; _extremeTime = time; _lookingForMax = false;
                }
            }
            else
            {
                if (value < _extremeVal) { _extremeVal = value; _extremeTime = time; }        // tieferes Minimum
                else if (value - _extremeVal >= ProminenceCm)                                 // Umkehr bestaetigt Min
                {
                    OnExtremeConfirmed(isMax: false, _extremeTime, _extremeVal);
                    _extremeVal = value; _extremeTime = time; _lookingForMax = true;
                }
            }
        }

        private void OnExtremeConfirmed(bool isMax, double time, double val)
        {
            if (_hasLastExtreme)
            {
                double amp = Math.Abs(val - _lastExtremeVal); // Peak-to-Trough = Kompressionstiefe
                if (amp >= MinCompressionAmplitudeCm)
                {
                    _lastCompressionTime = time;

                    _depthSamples.Enqueue((time, amp));
                    TrimByTime(_depthSamples, time, DepthWindowSec);

                    // Rate nur ueber Maxima-Abstaende (jede Kompression erzeugt 1 Max + 1 Min -> sonst doppelt gezaehlt)
                    if (isMax && (!_hasLastMaxTime || time - _lastMaxTime >= MinCompressionPeriodSec))
                    {
                        _maxTimes.Enqueue(time);
                        _lastMaxTime = time;
                        _hasLastMaxTime = true;
                        TrimByTime(_maxTimes, time, RateWindowSec);
                    }
                }
            }

            _hasLastExtreme = true;
            _lastExtremeVal = val;
        }


        // Emission --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void MaybeEmit(double now)
        {
            if (now - _lastEmitSec < EmitIntervalSec) return;
            _lastEmitSec = now;

            bool compressionDetected = (now - _lastCompressionTime) <= NoFlowTimeoutSec;

            double? cpm = compressionDetected ? ComputeCpm() : null;
            double? depth = compressionDetected ? ComputeDepth() : null;

            AnalysisCompleted?.Invoke(this, new AnalysisResult
            {
                CompressionDetected = compressionDetected,
                Frequency = cpm,
                Depth = depth,
                RateState = ClassifyRate(cpm),
                DepthState = ClassifyDepth(depth),
                ReleaseState = null // im SP-Modus nicht bestimmt
            });
        }

        private double? ComputeCpm()
        {
            if (_maxTimes.Count < 2) return null;
            double span = _lastMaxTime - _maxTimes.Peek();
            if (span <= 0) return null;
            return (_maxTimes.Count - 1) / span * 60.0;
        }

        private double? ComputeDepth()
        {
            if (_depthSamples.Count == 0) return null;
            double sum = 0;
            foreach (var d in _depthSamples) sum += d.amp;
            return sum / _depthSamples.Count;
        }

        private static StateOfRate ClassifyRate(double? cpm)
        {
            if (!cpm.HasValue) return StateOfRate.Optimal; // neutral; wird bei NoCPR ohnehin ignoriert
            if (cpm < RateOptimalMinCpm) return StateOfRate.TooSlow;
            if (cpm > RateOptimalMaxCpm) return StateOfRate.TooFast;
            return StateOfRate.Optimal;
        }

        private static StateOfDepth ClassifyDepth(double? depthCm)
        {
            if (!depthCm.HasValue) return StateOfDepth.Optimal;
            if (depthCm < DepthOptimalMinCm) return StateOfDepth.TooShallow;
            if (depthCm > DepthOptimalMaxCm) return StateOfDepth.TooDeep;
            return StateOfDepth.Optimal;
        }

        // Zwei konkrete Trim-Helfer (je Queue-Typ) ---------------------------------------------------------------------------------------------------------------------------------------------------
        private static void TrimByTime(Queue<double> q, double now, double window)
        {
            while (q.Count > 0 && now - q.Peek() > window) q.Dequeue();
        }

        private static void TrimByTime(Queue<(double time, double amp)> q, double now, double window)
        {
            while (q.Count > 0 && now - q.Peek().time > window) q.Dequeue();
        }


        // Reset (Instanz wird als Singleton zwischen Sessions wiederverwendet) ---------------------------------------------------------------------------------------------------------------------
        public void Reset()
        {
            _clock.Reset();
            _prevSampleSec = 0;
            _hasPrevSample = false;

            Array.Clear(_dtBuffer);
            _dtIndex = 0; _dtCount = 0; _fs = 0;

            _lp.Reset(); _hp1.Reset(); _hp2.Reset();
            _int1.Reset(); _int2.Reset();

            Array.Clear(_offsetBuffer);
            _offsetIndex = 0; _offsetCount = 0;

            _zigInit = false; _lookingForMax = true;
            _extremeVal = 0; _extremeTime = 0;
            _hasLastExtreme = false; _lastExtremeVal = 0;

            _hasLastMaxTime = false; _lastMaxTime = 0;
            _maxTimes.Clear();
            _depthSamples.Clear();

            _lastCompressionTime = double.NegativeInfinity;
            _lastEmitSec = 0;
        }


        // IIR-Filter 2.Ordnung -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private sealed class ButterworthFilter
        {
            private OnlineFilter? _filter;
            private double _fs, _fc;
            private bool _isLowpass;

            public void SetLowpass(double fs, double fc)
            {
                _fs = fs; _fc = fc; _isLowpass = true;
                _filter = OnlineFilter.CreateLowpass(
                    ImpulseResponse.Infinite, fs, fc, order: 2);
            }

            public void SetHighpass(double fs, double fc)
            {
                _fs = fs; _fc = fc; _isLowpass = false;
                _filter = OnlineFilter.CreateHighpass(
                    ImpulseResponse.Infinite, fs, fc, order: 2);
            }

            public double Process(double input)
                => _filter?.ProcessSample(input) ?? 0.0;  // ← null-guard nötig

            public void Reset()
            {
                if (_filter == null) return;  // noch nie konfiguriert
                if (_isLowpass)
                    _filter = OnlineFilter.CreateLowpass(
                        ImpulseResponse.Infinite, _fs, _fc, order: 2);
                else
                    _filter = OnlineFilter.CreateHighpass(
                        ImpulseResponse.Infinite, _fs, _fc, order: 2);
            }
        }

        // Kumulative Trapez-Integration mit variablem dt. ----------------------------------------------------------------------------------------------------------------------------------
        private sealed class TrapezoidIntegrator
        {
            private double _prev;
            private bool _hasPrev;
            private double _acc;

            public double Integrate(double value, double dt)
            {
                if (!_hasPrev) { _prev = value; _hasPrev = true; return 0.0; }
                _acc += 0.5 * (_prev + value) * dt;
                _prev = value;
                return _acc;
            }

            public void Reset() { _prev = 0; _hasPrev = false; _acc = 0; }
        }
    }
}