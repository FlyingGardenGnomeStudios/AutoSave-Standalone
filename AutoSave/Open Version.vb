Imports System.IO

Public Class Open_Version
    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        Try
            If rbOpenOld.Checked = True Then
                Me.Close()
            ElseIf rbRestore.Checked = True Then
                Kill(lblOriginal.Text)
                g_inventorApplication.ActiveDocument.SaveAs(lblOriginal.Text, True)
                My.Settings.CloseLater = g_inventorApplication.ActiveDocument.FullDocumentName
                My.Settings.Kill = True
                g_inventorApplication.Documents.Open(lblOriginal.Text)
                g_inventorApplication.ActiveDocument.PropertySets.Item("{32853F0F-3444-11D1-9E93-0060B03C1CA6}").ItemByPropId("5").Value = IO.Path.GetFileName(lblOriginal.Text)

            ElseIf rbOpenCurrent.Checked = True Then
                My.Settings.CloseLater = g_inventorApplication.ActiveDocument.FullDocumentName
                g_inventorApplication.Documents.Open(lblOriginal.Text)
            End If
        Catch ex As Exception
            Windows.Forms.MessageBox.Show(ex.Message)
        Finally
        End Try
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub Open_Version_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Try
            If My.Settings.CloseLater <> "" Then
                g_inventorApplication.Documents.ItemByName(My.Settings.CloseLater).Close(True)
                If My.Settings.Kill = True Then
                    Dim Read As IO.FileInfo
                    Read = My.Computer.FileSystem.GetFileInfo(My.Settings.CloseLater)
                    Read.IsReadOnly = False
                    Kill(My.Settings.CloseLater)
                End If
            End If
        Catch ex As Exception
            ' MessageBox.Show(ex.Message)
        Finally
            My.Settings.CloseLater = ""
            My.Settings.Kill = False
            My.Settings.Save()
        End Try
    End Sub
End Class