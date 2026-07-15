using Microsoft.Extensions.Logging;
using Live_CPR_Feedback_App.Services;
using Live_CPR_Feedback_App.ViewModels;
using Live_CPR_Feedback_App.Views;



namespace Live_CPR_Feedback_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });




            // Services 
            builder.Services.AddSingleton<IAccelerometerService, AccSensorDataService>();
            builder.Services.AddSingleton<IAnalysisServiceFactory, AnalysisServiceFactory>();
            builder.Services.AddTransient<ICPRSessionStorageService, CPRSessionStorageService>();
            builder.Services.AddSingleton<AnalysisServiceKI>();
            builder.Services.AddSingleton<AnalysisServiceSP>();
            builder.Services.AddSingleton<FeedbackService>();
            builder.Services.AddSingleton<CPRSessionStateService>();


            // ViewModels 
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddTransient<CPRViewModel>();
            builder.Services.AddTransient<InfoSensorViewModel>();
            builder.Services.AddTransient<StorageViewModel>();


            // Views 
            builder.Services.AddTransient<InfoPageSensor>();
            builder.Services.AddTransient<CPRPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<StoragePage>();






#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
