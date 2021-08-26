using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Resources;

namespace MindCanvas
{
    public static class Toast
    {
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        /// <summary>请求评价</summary>
        public static void RequestRatingsAndReviews()
        {
            new ToastContentBuilder()
                .AddArgument("action", "RequestReviews")
                .AddText(resourceLoader.GetString("Code_RequestRatingsAndReviewsToastText1"))// 为 MindCanvas 评分
                .AddText(resourceLoader.GetString("Code_RequestRatingsAndReviewsToastText2"))// 您对 MindCanvas 满意吗？
                .Show();
        }
    }
}
