Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data

Namespace SidhawanForProfsAndAdmins

    Partial Public Class PersonView
        Inherits UserControl

        Public ReadOnly Property Popup As MessageDialog
            Get
                Return PopupDialog
            End Get
        End Property

        Private Shared ReadOnly PersonalFields As (String, String)() = {
            ("First Name", NameOf(Person.FirstName)),
            ("Middle Name", NameOf(Person.MiddleName)),
            ("Last Name", NameOf(Person.LastName)),
            ("Suffix", NameOf(Person.Suffix)),
            ("Birth Date", NameOf(Person.BirthDate)),
            ("Sex", NameOf(Person.Sex)),
            ("Contact Number", NameOf(Person.ContactNumber)),
            ("Email Address", NameOf(Person.Email))
        }

        Private ReadOnly _pressBack As Func(Of UserControl)
        Private ReadOnly _person As Person
        Private ReadOnly _originalId As String
        Private ReadOnly _isStudent As Boolean

        Public Sub New(title As String, idLabel As String, person As Person,
                       Optional canEdit As Boolean = False,
                       Optional back As Func(Of UserControl) = Nothing)
            InitializeComponent()
            TitleText.Text = title

            IdField.Label = idLabel
            IdField.IsEditable = False
            Bind(IdField, person, NameOf(person.Id))

            For Each entry In PersonalFields
                Dim infoField = New InfoField With {.Label = entry.Item1}
                Bind(infoField, person, entry.Item2)
                PersonalGrid.Children.Add(infoField)
            Next

            For Each item In person.Academic
                Dim infoField = New InfoField With {.Label = item.Label}
                Bind(infoField, item, "Value")
                AcademicGrid.Children.Add(infoField)
            Next

            EditBtn.Visibility = If(canEdit, Visibility.Visible, Visibility.Collapsed)
            ConfirmBtn.Visibility = Visibility.Collapsed

            _pressBack = back
            BackBtn.Visibility = If(back IsNot Nothing, Visibility.Visible, Visibility.Collapsed)
            _person = person
            _originalId = person.Id
            _isStudent = (idLabel = "Student Number")
        End Sub

        Private Shared Sub Bind(field As InfoField, source As Object, path As String)
            Dim binding = New Binding(path)
            binding.Source = source
            binding.Mode = BindingMode.TwoWay
            field.SetBinding(InfoField.ValueProperty, binding)
        End Sub

        Private Sub Back_Click(sender As Object, e As RoutedEventArgs)
            If _pressBack IsNot Nothing Then
                Dim win = TryCast(Window.GetWindow(Me), Window2)
                If win IsNot Nothing Then win.Navigate(_pressBack())
            End If
        End Sub

        Private Sub Edit_Click(sender As Object, e As RoutedEventArgs)
            Ui.ToggleEdit(Me)

            IdField.IsEditable = False

            EditBtn.Visibility = Visibility.Collapsed
            ConfirmBtn.Visibility = Visibility.Visible
            ConfirmBtn.Content = "Confirm Edit"
        End Sub

        Private Sub Confirm_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrWhiteSpace(Data.Admin.Id) Then
                Popup.ShowNotice(
                    "cross",
                    "The administrator ID was not loaded. Changes were not saved.")
                Return
            End If

            Try
                Dim saved As Boolean

                If _isStudent Then
                    saved = Database.UpdateStudentProfile(
                        _person,
                        _originalId,
                        Data.Admin.Id)
                Else
                    saved = Database.UpdateEmployeeProfile(
                        _person,
                        _originalId)
                End If

                If Not saved Then
                    Popup.ShowNotice(
                        "cross",
                        "No profile was updated. Check that the profile ID matches the database key.")
                    Return
                End If

                If Not _isStudent Then
                    If Data.Admin.Id = _originalId Then
                        Database.LogOwnProfileUpdate(Data.Admin.Id)
                    Else
                        Database.LogEmployeeProfileUpdate(
                            Data.Admin.Id,
                            _originalId)
                    End If
                End If

                Database.ReloadHistory()

                Ui.ToggleEdit(Me)
                IdField.IsEditable = False
                ConfirmBtn.Visibility = Visibility.Collapsed
                EditBtn.Visibility = Visibility.Visible

                Popup.ShowNotice(
                    "check",
                    "Saved changes and updated history.")
            Catch ex As Exception
                Popup.ShowNotice(
                    "cross",
                    "The save or history update failed: " & ex.Message)
            End Try
        End Sub

    End Class
End Namespace