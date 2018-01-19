Imports System.IO

Public Class Open_Version
    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        Try
            If rbOpenOld.Checked = True Then
                Me.Close()
            ElseIf rbRestore.Checked = True Then
                Kill(lblOriginal.Text)
                g_inventorApplication.ActiveDocument.SaveAs(lblOriginal.Text, False)
                g_inventorApplication.Documents.ItemByName(lblOriginal.Text).Close(True)
                g_inventorApplication.Documents.Open(lblOriginal.Text)
                g_inventorApplication.ActiveDocument.PropertySets.Item("{32853F0F-3444-11D1-9E93-0060B03C1CA6}").ItemByPropId("5").Value = IO.Path.GetFileName(lblOriginal.Text)
            ElseIf rbOpenCurrent.Checked = True Then
                g_inventorApplication.ActiveDocument.Close(True)
                g_inventorApplication.Documents.Open(lblOriginal.Text)
            End If
        Catch ex As Exception
            Windows.Forms.MessageBox.Show(ex.Message)
        Finally
        End Try
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
        g_inventorApplication.ActiveDocument.Close(True)
    End Sub
End Class