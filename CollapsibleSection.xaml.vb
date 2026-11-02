Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace SidhawanForProfsAndAdmins

    Public Partial Class CollapsibleSection
        Inherits UserControl
        Public Shared ReadOnly TitleProperty As DependencyProperty = DependencyProperty.Register(NameOf(CollapsibleSection.Title), GetType(String), GetType(CollapsibleSection), New PropertyMetadata("", Sub(d, e) CType(d, CollapsibleSection).TitleText.Text = CStr(e.NewValue)))

        Public Shared ReadOnly BodyProperty As DependencyProperty = DependencyProperty.Register(NameOf(CollapsibleSection.Body), GetType(Object), GetType(CollapsibleSection), New PropertyMetadata(Nothing, Sub(d, e) CType(d, CollapsibleSection).BodyHost.Content = e.NewValue))

        Public Shared ReadOnly IsExpandedProperty As DependencyProperty = DependencyProperty.Register(NameOf(CollapsibleSection.IsExpanded), GetType(Boolean), GetType(CollapsibleSection), New PropertyMetadata(True, Sub(d, e) CType(d, CollapsibleSection).Apply(e.NewValue)))

        Public Property Title As String
            Get
                Return CStr(GetValue(TitleProperty))
            End Get
            Set(value As String)
                SetValue(TitleProperty, value)
            End Set
        End Property
        Public Property Body As Object
            Get
                Return GetValue(BodyProperty)
            End Get
            Set(value As Object)
                SetValue(BodyProperty, value)
            End Set
        End Property
        Public Property IsExpanded As Boolean
            Get
                Return GetValue(IsExpandedProperty)
            End Get
            Set(value As Boolean)
                SetValue(IsExpandedProperty, value)
            End Set
        End Property

        Public Sub New()
            Me.InitializeComponent()
        End Sub

        Private Sub Apply(open As Boolean)
            Me.BodyHost.Visibility = If(open, Visibility.Visible, Visibility.Collapsed)
            Me.Rot.Angle = If(open, 0, 180)
        End Sub

        Private Sub Header_Click(sender As Object, e As MouseButtonEventArgs)
            IsExpanded = Not IsExpanded
        End Sub
    End Class
End Namespace
