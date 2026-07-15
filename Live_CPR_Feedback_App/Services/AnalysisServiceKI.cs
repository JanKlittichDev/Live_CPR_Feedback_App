using Live_CPR_Feedback_App.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Live_CPR_Feedback_App.Services
{
    // KI-basierte Analyse: zwei ONNX-Modelle (depth + frequency), je 200 Roh-Magnitude-Samples als Input.
    // Samples kommen per AddSample() rein (kein direkter Sensor-Zugriff).
    // Sliding Window (Größe 200 Werte) mit Sprung alle 100 Werte (Stride).
    // Inferenz im Hintergrund, Ergebnis per Event AnalysisCompleted.
    //
    // bei Sample 300: _buffer-Array-Zustand ist dann:
    // Index 0–99: enthalten Samples 201–300 (die neuesten 100)
    // Index 100–199: enthalten Samples 101–200 (noch nicht überschrieben, weil die erst bei Sample 301 - 400 dran wären)
    // "Bruch" zwischen Index 99 / Sample 300   und   Index 100 / Sample 101: wird durch GetOrderedBuffer korrigiert
    //
    // Klassenkodierung (Softmax-ArgMax):
    //   Depth-Modell:     0 = zu flach, 1 = OK, 2 = zu tief, 3 = keine CPR
    //   Frequency-Modell: 0 = zu langsam, 1 = OK, 2 = zu schnell, 3 = keine CPR
    //

    public class AnalysisServiceKI : IAnalysisService
    {
        // Konfiguration ------------------------------------------------------------------------------------------
        private const int WindowSize = 200;   // Modell-Input-Laenge (muss zum Training passen!)
        private const int Stride = 100;        // alle N neuen Samples neu auswerten 

        private const string DepthModelFile = "depth_model.onnx";
        private const string FrequencyModelFile = "frequency_model.onnx";

        // Ringpuffer ---------------------------------------------------------------------------------------------
        private readonly float[] _buffer = new float[WindowSize];
        private int _bufferIndex = 0;
        private int _bufferFilled = 0;
        private int _samplesSinceLastRun = 0;

        // ONNX -----------------------------------------------------------------------------------------------------
        private InferenceSession? _depthSession;
        private InferenceSession? _frequencySession;
        private bool _modelsLoaded;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        // Re-Entrancy: nur eine Inferenz gleichzeitig
        private volatile bool _inferenceRunning;

        public event EventHandler<AnalysisResult>? AnalysisCompleted;


        // Sample rein -> Ringpuffer fuellen, ggf. Inferenz anstossen ------------------------------------------------
        public void AddSample(AccSensorData sample)
        {
            float magnitude = (float)Math.Sqrt(sample.X * sample.X + sample.Y * sample.Y + sample.Z * sample.Z);

            _buffer[_bufferIndex] = magnitude;
            _bufferIndex = (_bufferIndex + 1) % WindowSize;
            if (_bufferFilled < WindowSize) _bufferFilled++;

            _samplesSinceLastRun++;

            // Erst wenn Fenster voll ist, im Stride-Takt, und keine Inferenz laeuft
            if (_bufferFilled >= WindowSize && _samplesSinceLastRun >= Stride && !_inferenceRunning)
            {
                _inferenceRunning = true;      // hier setzen, um Doppel-Start zu vermeiden
                _samplesSinceLastRun = 0;

                float[] window = GetOrderedBuffer(); // Snapshot -> Ringpuffer darf weiterlaufen
                _ = RunInferenceAsync(window);       // fire-and-forget im Hintergrund
            }
        }


        // Inferenz (Hintergrund) -----------------------------------------------------------------------------------
        private async Task RunInferenceAsync(float[] window)
        {
            try
            {
                await EnsureModelsLoadedAsync();
                if (_depthSession is null || _frequencySession is null) return;

                float[] depthOut = Predict((float[])window.Clone(), _depthSession);  // Predict zentriert in-place -> Clone
                float[] freqOut = Predict((float[])window.Clone(), _frequencySession); 

                int depthClass = ArgMax(depthOut);  // 0=OK,1=flach,2=tief,3=keineCPR
                int freqClass = ArgMax(freqOut);   // 0=OK,1=langsam,2=schnell,3=keineCPR

                AnalysisResult result = MapToResult(depthClass, freqClass);
                AnalysisCompleted?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AnalysisServiceKI.RunInferenceAsync: {ex}");
            }
            finally
            {
                _inferenceRunning = false;
            }
        }


        // Zuordnung: Klassen -> AnalysisResult --------------------------------------------------------------------------------
        private static AnalysisResult MapToResult(int depthClass, int freqClass)
        {
            bool noCPR = depthClass == 3 || freqClass == 3;

            if (noCPR)
            {
                return new AnalysisResult
                {
                    CompressionDetected = false,
                    Frequency = null,   // KI liefert keine konkreten Zahlen, nur Klassen
                    Depth = null,
                    RateState = StateOfRate.TooSlow,
                    DepthState = StateOfDepth.TooShallow,
                    ReleaseState = null // KI-Modus kennt keine Release-Bewertung
                };
            }

            StateOfDepth depthState = depthClass switch
            {
                2 => StateOfDepth.TooDeep,
                1 => StateOfDepth.Optimal,
                _ => StateOfDepth.TooShallow
            };

            StateOfRate rateState = freqClass switch
            {
                2 => StateOfRate.TooFast,
                1 => StateOfRate.Optimal,
                _ => StateOfRate.TooSlow
            };

            return new AnalysisResult
            {
                CompressionDetected = true,
                Frequency = null,
                Depth = null,
                RateState = rateState,
                DepthState = depthState,
                ReleaseState = null
            };
        }


        // ONNX-Vorhersage ------------------------------------------------------------------------------------------
        private static float[] Predict(float[] input, InferenceSession session)
        {
            if (input.Length != WindowSize)
                throw new ArgumentException($"Die KI erwartet genau {WindowSize} Werte.");

            // Zentrierung (mean-subtraction) - MUSS zum Preprocessing im Training passen!
            float mean = input.Average();
            for (int i = 0; i < input.Length; i++)
                input[i] -= mean;

            var tensor = new DenseTensor<float>(input, new[] { 1, WindowSize });
            string inputName = session.InputMetadata.Keys.First();

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            };

            using var results = session.Run(inputs);
            return results.First().AsEnumerable<float>().ToArray();
        }


        // Model-Load (lazy, einmalig) ------------------------------------------------------------------------------
        private async Task EnsureModelsLoadedAsync()
        {
            if (_modelsLoaded) return;

            await _initLock.WaitAsync();
            try
            {
                if (_modelsLoaded) return; // Double-Checked

                byte[] depthBytes = await LoadModelBytesAsync(DepthModelFile);
                _depthSession = new InferenceSession(depthBytes);

                byte[] freqBytes = await LoadModelBytesAsync(FrequencyModelFile);
                _frequencySession = new InferenceSession(freqBytes);

                _modelsLoaded = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        // ONNX-Dateien liegen in Resources\Raw und muessen als MauiAsset markiert sein.
        private static async Task<byte[]> LoadModelBytesAsync(string fileName)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }


        // Helper ---------------------------------------------------------------------------------------------------
        private float[] GetOrderedBuffer()
        {
            float[] ordered = new float[WindowSize];
            int start = _bufferIndex; // aeltestes Element
            for (int i = 0; i < WindowSize; i++)
                ordered[i] = _buffer[(start + i) % WindowSize];
            return ordered;
        }

        private static int ArgMax(float[] arr)
        {
            int best = 0;
            for (int i = 1; i < arr.Length; i++)
                if (arr[i] > arr[best]) best = i;
            return best;
        }


        // Session-Ende ---------------------------------------------------------------------------------------------
        public void Reset()
        {
            // Nur Puffer-State zuruecksetzen. Sessions bleiben geladen (Singleton, Wiederverwendung).
            _bufferIndex = 0;
            _bufferFilled = 0;
            _samplesSinceLastRun = 0;
        }
    }
}



