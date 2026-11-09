using System.Windows;
using System.Windows.Media;

namespace CoachDesk.Desktop.Views;

public partial class PhotoPreviewWindow : Window
{
    public PhotoPreviewWindow(ImageSource image, string title)
    {
        InitializeComponent();
        PhotoTitle.Text = title;
        PreviewImage.Source = image;
    }
}
