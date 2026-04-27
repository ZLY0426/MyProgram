using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyProgram.CustomControl
{
    public class AnimatedContentControl : ContentControl
    {
        private ContentPresenter _oldContentPresenter;
        private ContentPresenter _newContentPresenter;
        private bool _isAnimating;

        static AnimatedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedContentControl),
                new FrameworkPropertyMetadata(typeof(AnimatedContentControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _oldContentPresenter = GetTemplateChild("OldContentPresenter") as ContentPresenter;
            _newContentPresenter = GetTemplateChild("NewContentPresenter") as ContentPresenter;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (_oldContentPresenter == null || _newContentPresenter == null || _isAnimating)
            {
                _newContentPresenter.Content = newContent;
                return;
            }

            AnimateTransition(oldContent, newContent);
        }

        private void AnimateTransition(object oldContent, object newContent)
        {
            _isAnimating = true;

            // 1. 设置旧内容初始状态（正常显示，margin = 0）
            _oldContentPresenter.Content = oldContent;
            _oldContentPresenter.Opacity = 1;
            _oldContentPresenter.Margin = new Thickness(0);

            // 2. 设置新内容初始状态（在右侧，透明）
            _newContentPresenter.Content = newContent;
            _newContentPresenter.Opacity = 0;
            _newContentPresenter.Margin = new Thickness(500, 0, 0, 0);

            // 3. 创建动画时间线
            var duration = TimeSpan.FromMilliseconds(500);

            // 旧页面：向左滑出并淡出（Margin.Left 从 0 到 -500）
            var oldFadeOut = new DoubleAnimation(1, 0, duration);
            var oldSlideOut = new ThicknessAnimation(
                new Thickness(0),
                new Thickness(-500, 0, 0, 0),
                duration);

            Storyboard.SetTarget(oldFadeOut, _oldContentPresenter);
            Storyboard.SetTarget(oldSlideOut, _oldContentPresenter);
            Storyboard.SetTargetProperty(oldFadeOut, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(oldSlideOut, new PropertyPath(MarginProperty));

            // 新页面：从右侧滑入并淡入（Margin.Left 从 500 到 0）
            var newFadeIn = new DoubleAnimation(0, 1, duration);
            var newSlideIn = new ThicknessAnimation(
                new Thickness(500, 0, 0, 0),
                new Thickness(0),
                duration);

            Storyboard.SetTarget(newFadeIn, _newContentPresenter);
            Storyboard.SetTarget(newSlideIn, _newContentPresenter);
            Storyboard.SetTargetProperty(newFadeIn, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(newSlideIn, new PropertyPath(MarginProperty));

            // 4. 播放动画
            var sb = new Storyboard();
            sb.Children.Add(oldFadeOut);
            sb.Children.Add(oldSlideOut);
            sb.Children.Add(newFadeIn);
            sb.Children.Add(newSlideIn);

            sb.Completed += (s, e) =>
            {
                _oldContentPresenter.Content = null;
                _isAnimating = false;
            };

            sb.Begin();
        }
    }
}
