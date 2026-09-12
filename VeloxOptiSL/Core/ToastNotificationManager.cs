using Microsoft.Toolkit.Uwp.Notifications;
using System;

namespace VeloxOptiSL
{
    public static class ToastNotificationManager
    {
        public static void ShowSuccess(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title, hintMaxLines: 1)
                .AddText(message)
                .Show();
        }

        public static void ShowWarning(string title, string message)
        {
            new ToastContentBuilder()
                .AddText("⚠️ " + title, hintMaxLines: 1)
                .AddText(message)
                .Show();
        }
    }
}