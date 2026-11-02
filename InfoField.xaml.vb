Imports System.Windows
Imports System.Windows.Controls

Namespace SidhawanForProfsAndAdmins

    Public Partial Class InfoField
        Inherits UserControl
        Public Shared ReadOnly LabelProperty As DependencyProperty = DependencyProperty.Register(NameOf(InfoField.Label), GetType(String), GetType(InfoField), New PropertyMetadata("", Sub(obj, e) CType(obj, InfoField).LabelText.Text = CStr(e.NewValue)))

        Public Shared ReadOnly ValueProperty As DependencyProperty = DependencyProperty.Register(NameOf(InfoField.Value), GetType(String), GetType(InfoField), New PropertyMetadata("", Sub(obj, e) CType(obj, InfoField).ValueBox.Text = CStr(e.NewValue)))

        Public Shared ReadOnly IsEditableProperty As DependencyProperty = DependencyProperty.Register(NameOf(InfoField.IsEditable), GetType(Boolean), GetType(InfoField), New PropertyMetadata(False, Sub(obj, e) CType(obj, InfoField).ValueBox.IsReadOnly = Not CBool(e.NewValue)))
        Public Property Label As String
            Get
                Return CStr(GetValue(LabelProperty))
            End Get
            Set(value As String)
                SetValue(LabelProperty, value)
            End Set
        End Property
        Public Property Value As String
            Get
                Return CStr(GetValue(ValueProperty))
            End Get
            Set(value As String)
                SetValue(ValueProperty, value)
            End Set
        End Property
        Public Property IsEditable As Boolean
            Get
                Return GetValue(IsEditableProperty)
            End Get
            Set(value As Boolean)
                SetValue(IsEditableProperty, value)
            End Set
        End Property
        Public Sub New()
            Me.InitializeComponent()
            AddHandler Me.ValueBox.TextChanged, Sub(src, args) Value = Me.ValueBox.Text
        End Sub
    End Class
End Namespace
