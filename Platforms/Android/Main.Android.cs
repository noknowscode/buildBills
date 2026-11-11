using Android.App;
using Android.Content.PM;
using Android.Runtime;

namespace UnoCompletionAnimations;

[Activity(
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation |
                           ConfigChanges.ScreenSize |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.UiMode |
                           ConfigChanges.Keyboard |
                           ConfigChanges.KeyboardHidden |
                           ConfigChanges.FontScale |
                           ConfigChanges.Density)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
}

[Application]
public class AndroidApplication : Microsoft.UI.Xaml.NativeApplication
{
    public AndroidApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}