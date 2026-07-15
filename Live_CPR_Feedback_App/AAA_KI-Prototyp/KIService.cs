using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
namespace CPR_Feedback.Services
{
    public class KIService
    {
        private InferenceSession depthSession;
        private InferenceSession frequencySession;

        public KIService()
        {
        }

        public async Task InitializeAsync()
        {
            // 1. Depth Model laden 
            byte[] depthModelBytes = await LoadModelFromAppPackageAsync("depth_model.onnx");
            depthSession = new InferenceSession(depthModelBytes);

            // 2. Frequency Model laden 
            byte[] frequencyModelBytes = await LoadModelFromAppPackageAsync("frequency_model.onnx");
            frequencySession = new InferenceSession(frequencyModelBytes);
        }

        // Hilfsmethode, um Dateien aus der Handy-App zu lesen 
        private async Task<byte[]> LoadModelFromAppPackageAsync(string filename)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public float[] PredictDepth(float[] input)
        {
            return Predict(input, depthSession);
        }

        public float[] PredictFrequency(float[] input)
        {
            return Predict(input, frequencySession);
        }

        private float[] Predict(float[] input, InferenceSession session)
        {
            if (input.Length != 200)
                throw new ArgumentException("Die KI erwartet genau 200 Werte.");

            float mean = input.Average();

            for (int i = 0; i < input.Length; i++)
                input[i] = input[i] - mean;

            var tensor = new DenseTensor<float>(input, new[] { 1, 200 });

            string inputName = session.InputMetadata.Keys.First();

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            };

            using var results = session.Run(inputs);

            return results.First().AsEnumerable<float>().ToArray();
        }
    }
}