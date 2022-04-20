using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Aska.WPF.Controls
{
    /// <summary>
    /// FishEyePanel
    /// </summary>
    public class FishEyePanel : Panel
    {
        private enum AnimateState { None, Up, Down };

        public FishEyePanel()
        {
            Background = Brushes.Transparent;
            MouseMove += new MouseEventHandler(FishEyePanel_MouseMove);
            MouseEnter += new MouseEventHandler(FishEyePanel_MouseEnter);
            MouseLeave += new MouseEventHandler(FishEyePanel_MouseLeave);
        }

        public double Magnification
        {
            get { return (double)GetValue(MagnificationProperty); }
            set { SetValue(MagnificationProperty, value); }
        }

        public static readonly DependencyProperty MagnificationProperty =
            DependencyProperty.Register("Magnification", typeof(double),
                typeof(FishEyePanel), new UIPropertyMetadata(2d));

        public int AnimationMilliseconds
        {
            get { return (int)GetValue(AnimationMillisecondsProperty); }
            set { SetValue(AnimationMillisecondsProperty, value); }
        }

        public static readonly DependencyProperty AnimationMillisecondsProperty =
            DependencyProperty.Register("AnimationMilliseconds", typeof(int),
                typeof(FishEyePanel), new UIPropertyMetadata(125));

        public bool ScaleToFit
        {
            get { return (bool)GetValue(ScaleToFitProperty); }
            set { SetValue(ScaleToFitProperty, value); }
        }

        public static readonly DependencyProperty ScaleToFitProperty =
            DependencyProperty.Register("ScaleToFit", typeof(bool),
                typeof(FishEyePanel), new UIPropertyMetadata(true));

        private bool animating = false, wasMouseOver = false;
        private Size ourSize;
        private double totalChildWidth = 0;

        private void FishEyePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!animating) InvalidateArrange();
        }

        private void FishEyePanel_MouseEnter(object sender, MouseEventArgs e) => InvalidateArrange();

        private void FishEyePanel_MouseLeave(object sender, MouseEventArgs e) => InvalidateArrange();

        protected override Size MeasureOverride(Size availableSize)
        {
            Size idealSize = new(0, 0);

            Size size = new(Double.PositiveInfinity, Double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                child.Measure(size);
                idealSize.Width += child.DesiredSize.Width;
                idealSize.Height = Math.Max(idealSize.Height, child.DesiredSize.Height);
            }

            return double.IsInfinity(availableSize.Height)
                || double.IsInfinity(availableSize.Width)
                ? idealSize : availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children == null || Children.Count == 0) return finalSize;

            ourSize = finalSize;
            totalChildWidth = 0;

            foreach (UIElement child in Children)
            {
                if (child.RenderTransform as TransformGroup == null)
                {
                    child.RenderTransformOrigin = new(0, 0.5);
                    TransformGroup group = new();
                    child.RenderTransform = group;
                    group.Children.Add(new ScaleTransform());
                    group.Children.Add(new TranslateTransform());

                }

                child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
                totalChildWidth += child.DesiredSize.Width;
            }

            AnimateAll();

            return finalSize;
        }

        private void AnimateAll()
        {
            if (Children == null || Children.Count == 0) return;

            animating = true;

            double childWidth = ourSize.Width / Children.Count;

            double overallScaleFactor = ourSize.Width / totalChildWidth;

            UIElement? prevChild = null, theChild = null, nextChild = null;

            double widthSoFar = 0, theChildX = 0, ratio = 0;

            if (IsMouseOver)
            {
                double x = Mouse.GetPosition(this).X;
                foreach (UIElement child in Children)
                {
                    if (theChild == null) theChildX = widthSoFar;
                    widthSoFar += (ScaleToFit ? childWidth : child.DesiredSize.Width * overallScaleFactor);
                    if (x < widthSoFar && theChild == null) theChild = child;
                    if (theChild == null) prevChild = child;
                    if (nextChild == null && theChild != child && theChild != null)
                    {
                        nextChild = child;
                        break;
                    }
                }
                if (theChild != null)
                    ratio = (x - theChildX) / (ScaleToFit ? childWidth :
                        (theChild.DesiredSize.Width * overallScaleFactor));
            }

            double mag = Magnification, extra = 0;
            if (theChild != null) extra += (mag - 1);

            if (prevChild == null) extra += (ratio * (mag - 1));
            else if (nextChild == null) extra += ((mag - 1) * (1 - ratio));
            else extra += (mag - 1);

            double prevScale = Children.Count * (1 + ((mag - 1) * (1 - ratio))) / (Children.Count + extra);
            double theScale = (mag * Children.Count) / (Children.Count + extra);
            double nextScale = Children.Count * (1 + ((mag - 1) * ratio)) / (Children.Count + extra);
            double otherScale = Children.Count / (Children.Count + extra);

            if (!ScaleToFit && IsMouseOver)
            {
                double bigWidth = 0;
                double actualWidth = 0;
                if (prevChild != null)
                {
                    bigWidth += prevScale * prevChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += prevChild.DesiredSize.Width;
                }
                if (theChild != null)
                {
                    bigWidth += theScale * theChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += theChild.DesiredSize.Width;
                }
                if (nextChild != null)
                {
                    bigWidth += nextScale * nextChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += nextChild.DesiredSize.Width;
                }
                double w = (totalChildWidth - actualWidth) * overallScaleFactor * otherScale;
                otherScale *= (ourSize.Width - bigWidth) / w;
            }

            widthSoFar = 0;
            double duration = 0;
            if (wasMouseOver != IsMouseOver) duration = AnimationMilliseconds;

            foreach (UIElement child in Children)
            {
                double scale = otherScale;
                if (child == prevChild) scale = prevScale;
                else if (child == theChild) scale = theScale;
                else if (child == nextChild) scale = nextScale;

                if (ScaleToFit) scale *= childWidth / child.DesiredSize.Width;
                else scale *= overallScaleFactor;

                AnimateTo(child, widthSoFar, (ourSize.Height - child.DesiredSize.Height) / 2, scale, duration);
                widthSoFar += child.DesiredSize.Width * scale;
            }

            wasMouseOver = IsMouseOver;
        }

        private void AnimateTo(UIElement child, double x, double y, double s, double duration)
        {
            TransformGroup group = (TransformGroup)child.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform trans = (TranslateTransform)group.Children[1];

            if (duration == 0)
            {
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.YProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                trans.X = x;
                trans.Y = y;
                scale.ScaleX = s;
                scale.ScaleY = s;
                Animation_Completed(null, null);
            }
            else
            {
                trans.BeginAnimation(TranslateTransform.XProperty, MakeAnimation(x, duration, Animation_Completed));
                trans.BeginAnimation(TranslateTransform.YProperty, MakeAnimation(y, duration));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, MakeAnimation(s, duration));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, MakeAnimation(s, duration));
            }
        }

        private static DoubleAnimation MakeAnimation(double to, double duration) => MakeAnimation(to, duration, null);

        private static DoubleAnimation MakeAnimation(double to, double duration, EventHandler? endEvent)
        {
            DoubleAnimation anim = new(to, TimeSpan.FromMilliseconds(duration));
            anim.AccelerationRatio = 0.2;
            anim.DecelerationRatio = 0.7;
            if (endEvent != null) anim.Completed += endEvent;
            return anim;
        }

        private void Animation_Completed(object? sender, EventArgs? e) => animating = false;
    }
}