Imports System
Imports System.Diagnostics
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace SidhawanForProfsAndAdmins

    Public Class Icon
        Inherits Image
        Public Shared ReadOnly FileProperty As DependencyProperty = DependencyProperty.Register(NameOf(Icon.File), GetType(String), GetType(Icon), New PropertyMetadata("", Sub(d, e) CType(d, Icon).Load(CStr(e.NewValue))))

        Public Property File As String
            Get
                Return CStr(GetValue(FileProperty))
            End Get
            Set(value As String)
                SetValue(FileProperty, value)
            End Set
        End Property

        Private Shared ReadOnly Folders As String() = {"Images/Icon/", "Images/Icons/", "Images/", ""}
        Private Shared ReadOnly Extensions As String() = {"png", "jpg", "jpeg", "bmp", "gif"}

        Private Shared ReadOnly Missing As ImageSource = New DrawingImage(New GeometryDrawing(Brushes.Transparent, New Pen(Brushes.Red, 2), Geometry.Parse("M1,1 L23,1 L23,23 L1,23 Z M1,1 L23,23 M23,1 L1,23")))

        Public Sub New()
            Stretch = Stretch.Uniform
            RenderOptions.SetBitmapScalingMode(Me, BitmapScalingMode.HighQuality)
        End Sub

        Private Sub Load(name As String)
            If String.IsNullOrEmpty(name) Then
                Source = Nothing
                Return
            End If

            For Each folder In Folders
                For Each ext In Extensions
                    Try
                        Source = New BitmapImage(New Uri($"pack://application:,,,/{folder}{name}.{ext}"))
                        ToolTip = Nothing
                        Return
                    Catch
                    End Try
                Next
            Next

            Debug.WriteLine($"Icon '{name}' NOT FOUND.")
            Source = Missing
            ToolTip = $"Missing icon: {name}"
        End Sub
    End Class
End Namespace
