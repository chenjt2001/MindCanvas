using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
    public class MindMapInkCanvas : InkCanvas
    {
        public MindMapInkCanvas()
        {
            //InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            // 画布大小
            Width = InitialValues.MindMapCanvasWidth;
            Height = InitialValues.MindMapCanvasHeight;

            InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            InkPresenter.StrokesErased += InkPresenter_StrokesErased;
            //CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(this.InkPresenter);
            //core.PointerReleasing += Core_PointerReleasing;
        }

        // 删除一条线
        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            EventsManager.ModifyInkCanvas(sender.StrokeContainer);
            MainPage.mainPage.RefreshUnRedoBtn();
        }

        // 新增一条线
        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            EventsManager.ModifyInkCanvas(sender.StrokeContainer);
            MainPage.mainPage.RefreshUnRedoBtn();
        }
    }
}
