Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Linq

Namespace SidhawanForProfsAndAdmins

    Public Class Backend_Prof_GradeEdit

        Public Function LoadGrades(
            classId As String,
            Optional term As String = "",
            Optional component As String = "",
            Optional itemName As String = "") _
            As List(Of ProfessorItemizedGradeRow)

            Dim rows = Database.LoadProfessorItemizedGrades(classId)

            If rows Is Nothing Then
                Return New List(Of ProfessorItemizedGradeRow)()
            End If

            Return FilterGrades(rows, term, component, itemName)
        End Function

        Public Function FilterGrades(
            rows As IEnumerable(Of ProfessorItemizedGradeRow),
            term As String,
            component As String,
            itemName As String) _
            As List(Of ProfessorItemizedGradeRow)

            Return rows.Where(
                Function(row)
                    Dim termMatches =
                        String.IsNullOrWhiteSpace(term) OrElse
                        String.Equals(term, "All", StringComparison.OrdinalIgnoreCase) OrElse
                        String.Equals(row.Term, term, StringComparison.OrdinalIgnoreCase)

                    Dim componentMatches =
                        String.IsNullOrWhiteSpace(component) OrElse
                        String.Equals(component, "All", StringComparison.OrdinalIgnoreCase) OrElse
                        String.Equals(row.Component, component, StringComparison.OrdinalIgnoreCase)

                    Dim itemMatches =
                        String.IsNullOrWhiteSpace(itemName) OrElse
                        String.Equals(itemName, "All", StringComparison.OrdinalIgnoreCase) OrElse
                        String.Equals(row.ItemName, itemName, StringComparison.OrdinalIgnoreCase)

                    Return termMatches AndAlso componentMatches AndAlso itemMatches
                End Function).ToList()
        End Function

        Public Function GetTerms(
            rows As IEnumerable(Of ProfessorItemizedGradeRow)) _
            As List(Of String)

            Return rows.Select(Function(row) row.Term).
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value).
                ToList()
        End Function

        Public Function GetComponents(
            rows As IEnumerable(Of ProfessorItemizedGradeRow)) _
            As List(Of String)

            Return rows.Select(Function(row) row.Component).
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value).
                ToList()
        End Function

        Public Function GetItems(
            rows As IEnumerable(Of ProfessorItemizedGradeRow)) _
            As List(Of String)

            Return rows.Select(Function(row) row.ItemName).
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value).
                ToList()
        End Function

        Public Sub SaveScore(
            row As ProfessorItemizedGradeRow,
            value As String)

            value = If(String.IsNullOrWhiteSpace(value), "0", value.Trim())
            ValidateScore(row, value)

            Database.UpdateProfessorItemizedGrade(
                row.ClassItemID,
                row.StudentID,
                value)

            row.AcquiredScore = value
        End Sub

        Public Sub ValidateScore(
            row As ProfessorItemizedGradeRow,
            value As String)

            Dim score As Integer

            If Not String.IsNullOrWhiteSpace(value) AndAlso
                Not Integer.TryParse(
                    value.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    score) Then

                Throw New FormatException(
                    "Enter a valid score or leave the field blank.")
            End If

            If score < 0 Then
                Throw New InvalidOperationException(
                    "The score cannot be negative.")
            End If

            Dim highestScore As Integer
            If Integer.TryParse(
                row.HighestScore,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                highestScore) AndAlso score > highestScore Then

                Throw New InvalidOperationException(
                    "The score cannot exceed the highest score.")
            End If

        End Sub

    End Class

    Public Class AttendanceMatrixRow
        Public Property StudentID As String = ""
        Public Property Label As String = ""
        Public Property Cells As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public Property AttendanceIds As List(Of String) = New List(Of String)()
    End Class

End Namespace
