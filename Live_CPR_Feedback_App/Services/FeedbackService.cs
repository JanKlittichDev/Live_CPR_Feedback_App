using Plugin.Maui.Audio;
using Live_CPR_Feedback_App.Models;


namespace Live_CPR_Feedback_App.Services
{
    // Priorisierung + Audio
    public class FeedbackService  
    {

        private IAudioPlayer? player;
        private bool isSpeaking = false;
        



        // Hauptmethode: wird pro AnalysisResult vom ViewModel aufgerufen -----------------------------------------------------------------------------------------------------------------
        public async Task<FeedbackResultForUI> Present(AnalysisResult result)
        {
            FeedbackType type = Resolve(result);

            FeedbackResultForUI feedback = BuildFeedback(type);

            // Audio nur starten, wenn nicht "None" und aktuell nichts laeuft
            if (type != FeedbackType.None && !isSpeaking)
            {
                await PlaySound(GetSoundFile(type));
            }

            return feedback;
        }


        
        // Priorisierung: erste zutreffende Regel gewinnt, Rest wird uebersprungen -----------------------------------------------------------------------------------------------------------------
        private FeedbackType Resolve(AnalysisResult r)
        {
            // 1. No-CPR (hoechste Prioritaet)
            if (!r.CompressionDetected) return FeedbackType.NoCPR;
            
            // 2. Tiefe zu gering
            if (r.DepthState == StateOfDepth.TooShallow) return FeedbackType.PushDeeper;

            // 3. Tiefe zu hoch
            if (r.DepthState == StateOfDepth.TooDeep) return FeedbackType.TooDeep;

            // 4. Frequenz zu langsam
            if (r.RateState == StateOfRate.TooSlow) return FeedbackType.PushFaster;

            // 5. Frequenz zu schnell
            if (r.RateState == StateOfRate.TooFast) return FeedbackType.Slowdown;

            // 6. Unvollstaendige Entlastung (nur falls implementiert/vorhanden)
            if (r.ReleaseState == StateOfRelease.Incomplete) return FeedbackType.ReleaseFully;

            // 7. Nichts zutreffend -> alles gut
            return FeedbackType.None;
        }



        // Mapping Feedback-Typ: Anzeige-Text + Anzeige-Farbe (Gut/Schlecht) -------------------------------------------------------------------------------------------------------------------
        private FeedbackResultForUI BuildFeedback(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.None => new FeedbackResultForUI { Type = type, FeedbackText = "GOOD COMPRESSIONS", IsGood = true },
                FeedbackType.NoCPR => new FeedbackResultForUI { Type = type, FeedbackText = "CONTINUE COMPRESSIONS!", IsGood = false },
                FeedbackType.PushDeeper => new FeedbackResultForUI { Type = type, FeedbackText = "PUSH DEEPER!", IsGood = false },
                FeedbackType.TooDeep => new FeedbackResultForUI { Type = type, FeedbackText = "DO NOT PUSH TOO DEEP!", IsGood = false },
                FeedbackType.PushFaster => new FeedbackResultForUI { Type = type, FeedbackText = "PUSH FASTER!", IsGood = false },
                FeedbackType.Slowdown => new FeedbackResultForUI { Type = type, FeedbackText = "SLOW DOWN!", IsGood = false },
                FeedbackType.ReleaseFully => new FeedbackResultForUI { Type = type, FeedbackText = "RELEASE FULLY!", IsGood = false },
                _ => new FeedbackResultForUI { Type = FeedbackType.None, FeedbackText = "WAITING", IsGood = null },
            };
        }









        // Audio ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string GetSoundFile(FeedbackType type) => type switch
        {
            FeedbackType.NoCPR => "feedback_continue.mp3",
            FeedbackType.PushDeeper => "feedback_pushdeeper.mp3",
            FeedbackType.PushFaster => "feedback_pushfaster.mp3",
            FeedbackType.Slowdown => "feedback_slowdown.mp3",
            FeedbackType.ReleaseFully => "feedback_releasefully.mp3",
            _ => "",
        };

        private async Task PlaySound(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            isSpeaking = true;
            StopAudio();
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            player = AudioManager.Current.CreatePlayer(stream);
            player.Play();

            while (player != null && player.IsPlaying)
                await Task.Delay(50);

            isSpeaking = false;
        }

        public void StopAudio()
        {
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
        }

        public void Reset() // beim Verlassen der Seite aufrufen
        {
            StopAudio();
            isSpeaking = false;
        }
    }
}



