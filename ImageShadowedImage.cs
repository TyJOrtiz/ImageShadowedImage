using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Composition;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static System.Net.Mime.MediaTypeNames;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Foundation;
using CommunityToolkit.WinUI.Media.Helpers;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace ImageShadowedImage
{
    /// <summary>
    /// ShadowedImage control.
    /// </summary>
    public sealed class ImageShadowedImage : Control
    {
        private static Compositor _compositor;
        private Grid VisualGrid;
        private ImageEx.ImageEx MainImage;


        /// <summary>
        /// ShadowedImage control.
        /// </summary>
        public ImageShadowedImage()
        {
            this.DefaultStyleKey = typeof(ImageShadowedImage);
            this.Loaded += ShadowedImage_Loaded;
        }
        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            VisualGrid = this.GetTemplateChild("VisualGrid") as Grid;
            MainImage = this.GetTemplateChild("MainImage") as ImageEx.ImageEx;
        }
        private void ShadowedImage_Loaded(object sender, RoutedEventArgs e)
        {
            var accessibility = new AccessibilitySettings();
            accessibility.HighContrastChanged += (a, ch) =>
            {
                if (accessibility.HighContrast)
                {
                    VisualGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (this.ActualTheme == ElementTheme.Dark)
                    {
                        VisualGrid.Visibility = Visibility.Collapsed;
                        MainImage.Translation = new Vector3(0, 0, 32);
                    }
                    else
                    {
                        VisualGrid.Visibility = Visibility.Visible;
                        //Reload();
                        //VisualGrid.Translation = new Vector3(0, 5, 0);
                        MainImage.Translation = new Vector3(0, 0, 0);
                    }
                    this.ActualThemeChanged += (t, c) =>
                    {
                        if (this.ActualTheme == ElementTheme.Dark)
                        {
                            VisualGrid.Visibility = Visibility.Collapsed;
                            MainImage.Translation = new Vector3(0, 0, 32);
                        }
                        else
                        {
                            VisualGrid.Visibility = Visibility.Visible;
                            Reload();
                            //VisualGrid.Translation = new Vector3(0, 5, 0);
                            MainImage.Translation = new Vector3(0, 0, 0);
                        }
                    };
                }
            };
            if (accessibility.HighContrast)
            {
                VisualGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (this.ActualTheme == ElementTheme.Dark)
                {
                    VisualGrid.Visibility = Visibility.Collapsed;
                    MainImage.Translation = new Vector3(0, 0, 32);
                }
                else
                {
                    VisualGrid.Visibility = Visibility.Visible;
                    //Reload();
                    //VisualGrid.Translation = new Vector3(0, 5, 0);
                    MainImage.Translation = new Vector3(0, 0, 0);
                }
                this.ActualThemeChanged += (t, c) =>
                {
                    if (this.ActualTheme == ElementTheme.Dark)
                    {
                        VisualGrid.Visibility = Visibility.Collapsed;
                        MainImage.Translation = new Vector3(0, 0, 32);
                    }
                    else
                    {
                        VisualGrid.Visibility = Visibility.Visible;
                        Reload();
                        //BackImage.Translation = new Vector3(0, 5, 0);
                        MainImage.Translation = new Vector3(0, 0, 0);
                    }
                };
            }
            //Debug.WriteLine(ConvertImageToBase64("ms-appx:///Assets/ImageMask.Png"));
        }

        /// <summary>
        /// The Image source for the main image and shadow. can be string or url only.
        /// </summary>
        public object Source
        {
            get
            {
                return (object)GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty, value);
            }
        }
        /// <summary>
        /// The Image source for the main image and shadow. can be string or url only.
        /// </summary>
        private static DependencyProperty SourceProperty { get; } = DependencyProperty.Register("Source", typeof(object), typeof(ImageShadowedImage), new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));
        
        private Vector3 UpdateVisualPosition(SpriteVisual visual, float objectWidth, float objectHeight, float visualWidth, float visualHeight)
        {
            // Calculate dynamic offset to center the visual
            float offsetX = (objectWidth - visualWidth) / 2;
            float offsetY = (objectHeight - visualHeight) / 2 + (visualHeight * 0.05f);

            // Apply the offset to the visual
            return new Vector3(offsetX, offsetY, 0);
        }
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageShadowedImage)d;
            if (e.NewValue == null)
            {
                control.MainImage.Source = null;
            }
            else
            {
                if (e.NewValue is string path)
                {
                    if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                    {
                        control.MainImage.Source = new BitmapImage(new Uri(path));
                        control.Reload();
                        //control.BackImage.Source = new BitmapImage(new Uri(path));
                    }
                    else
                    {
                        throw new InvalidOperationException("Source must be a uri.");
                    }
                }
                else if (e.NewValue is Uri url)
                {
                    control.MainImage.Source = new BitmapImage(url);
                    control.Reload();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
        private async void Load()
        {

            if (Source != null)
            {
                VisualGrid.Visibility = Visibility.Visible;
                _compositor = ElementCompositionPreview.GetElementVisual(VisualGrid).Compositor;
                var _containerVisual = _compositor.CreateContainerVisual();
                var surface = LoadedImageSurface.StartLoadFromUri(new Uri(Source.ToString()), new Size((this.ActualHeight * 1), ((this.ActualWidth * 1))));
                //surface.
                var _shadowVisual = _compositor.CreateSpriteVisual();
                _shadowVisual.Size = new Vector2((float)((float)this.ActualHeight * 1.2), (float)((float)this.ActualWidth * 1.2));
                var shadowBrush = _compositor.CreateSurfaceBrush(surface);

                // load in our opacity mask image.
                var loader = SurfaceLoader.GetInstance();
                // this is created in a graphic tool such as paint.net
                var opacityMaskSurface = await loader.LoadFromUri(new Uri("ms-appx:///Assets/ImageMask.png"), new Size((float)((float)this.ActualHeight * 1.275), (float)((float)this.ActualWidth * 1.275)));

                // create surfacebrush with ICompositionSurface that contains the background image to be masked
                shadowBrush.Stretch = CompositionStretch.UniformToFill;

                // create surfacebrush with ICompositionSurface that contains the gradient opacity mask asset
                CompositionSurfaceBrush opacityBrush = _compositor.CreateSurfaceBrush(opacityMaskSurface);
                opacityBrush.Stretch = CompositionStretch.None;
                var blurEffect = new GaussianBlurEffect
                {
                    Source = new CompositionEffectSourceParameter("source"),
                    BlurAmount = 20f,
                    BorderMode = EffectBorderMode.Soft
                };
                var effectFactory = _compositor.CreateEffectFactory(blurEffect);
                var effectBrush = effectFactory.CreateBrush();
                effectBrush.SetSourceParameter("source", shadowBrush);

                var maskBrush = _compositor.CreateMaskBrush();
                maskBrush.Mask = opacityBrush;
                maskBrush.Source = effectBrush;
                _shadowVisual.Brush = maskBrush;
                _shadowVisual.Opacity = 1f;
                _shadowVisual.CenterPoint = new Vector3((float)_shadowVisual.Size.X / 2, (float)_shadowVisual.Size.Y / 2, 0);
                _shadowVisual.Offset = UpdateVisualPosition(_shadowVisual, (float)this.ActualWidth, (float)this.ActualHeight, _shadowVisual.Size.X, _shadowVisual.Size.Y);
                this.SizeChanged += (t, s) =>
                {
                    _shadowVisual.Size = new Vector2((float)((float)s.NewSize.Width * 1.2), (float)((float)s.NewSize.Height * 1.2));
                    _shadowVisual.CenterPoint = new Vector3((float)_shadowVisual.Size.X / 2, (float)_shadowVisual.Size.Y / 2, 0);
                    _shadowVisual.Offset = UpdateVisualPosition(_shadowVisual, (float)s.NewSize.Height, (float)s.NewSize.Height, _shadowVisual.Size.X, _shadowVisual.Size.Y);
                };
                //var _imageVisual = _compositor.CreateSpriteVisual();
                //_imageVisual.Size = new Vector2(100,100);
                //_imageVisual.Offset = new Vector3(25, 25, 0);
                //_imageVisual.Brush = _compositor.CreateSurfaceBrush(surface);
                // create maskbrush
                //CompositionMaskBrush maskbrush = _compositor.CreateMaskBrush();
                //maskbrush.Mask = opacityBrush; // surfacebrush with gradient opacity mask asset
                //maskbrush.Source = shadowBrush; // surfacebrush with background image that is to be masked
                _containerVisual.Children.InsertAtBottom(_shadowVisual);
                //_containerVisual.Children.InsertAtTop(_imageVisual);
                // create spritevisual of the approproate size, offset, etc.
                //SpriteVisual maskSprite = _compositor.CreateSpriteVisual();
                //maskSprite.Size = new Vector2((float)200, 200);
                //maskSprite.Brush = maskBrush; // paint it with the maskbrush
                ElementCompositionPreview.SetElementChildVisual(VisualGrid, _containerVisual);
            }
            else
            {
                VisualGrid.Visibility = Visibility.Collapsed;
            }
        }
        private void Reload()
        {
            if (this.IsLoaded)
            {
                var accessibility = new AccessibilitySettings();
                if (accessibility.HighContrast)
                {
                    //VisualGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (this.ActualTheme == ElementTheme.Dark)
                    {
                    }
                    else
                    {
                        VisualGrid.Visibility = Visibility.Visible;

                        Load();
                        //Reload();
                        //VisualGrid.Translation = new Vector3(0, 5, 0);
                        MainImage.Translation = new Vector3(0, 0, 0);
                    }
                }
            }
            else
            {
                this.Loaded += (t, l) =>
                {
                    var accessibility = new AccessibilitySettings();
                    if (accessibility.HighContrast)
                    {
                        //VisualGrid.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (this.ActualTheme == ElementTheme.Dark)
                        {
                        }
                        else
                        {
                            VisualGrid.Visibility = Visibility.Visible;

                            Load();
                            //Reload();
                            //VisualGrid.Translation = new Vector3(0, 5, 0);
                            MainImage.Translation = new Vector3(0, 0, 0);
                        }
                    }
                };
            }
        }
    }
}
