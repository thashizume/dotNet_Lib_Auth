Public Class Subscription

    Private Shadows Const connectionString As String = "Server=tcp:t5d2skdq62.database.windows.net,1433;Database=polestarsvc;User ID=polestaradmin@t5d2skdq62;Password=1Qaz2Wsx,;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;"

    Public Function getSubscriptionName(subs As System.Collections.Generic.Dictionary(Of String, String)) As System.Collections.Generic.Dictionary(Of String, String)

        Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
        Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
        Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString)

        Dim result As System.Collections.Generic.Dictionary(Of String, String) = subs

        Dim c As String = String.Empty
        For Each key As String In result.Keys
            c += String.Format("'{0}',", key)

        Next
        c = c.Trim(",")

        Dim sql As String = String.Format("select * from SUBSCRIPTION where INVALID=0 and  SUBSCRIPTION_CODE in ({0})", c)
        _db.Open()
        Dim dt As System.Data.DataTable = _db.DataReader2DataTable(_db.ExecuteQuery(sql))
        _db.Close()

        For Each row As System.Data.DataRow In dt.Rows
            result(row("SUBSCRIPTION_CODE")) = row("SUBSCRIPTION_NAME")
        Next

        Return result

    End Function

    Public Function add(fingerPrint As String, subscriptionName As String) As Dictionary(Of String, String)

        Dim result As New Dictionary(Of String, String)
        Dim _subscriptionCode As String = Me.genarateSubscriptionCode
        Dim _subscriptionName As String = (New Polestar.Security.Cryptography()).Decrypt(subscriptionName, fingerPrint)

        Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
        Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
        Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString, True)
        _db.Open()

        result.Add("FINGER_PRINT", _fingerPrint)

        Try
            Dim sql As String = String.Format("insert into SUBSCRIPTION( SUBSCRIPTION_CODE,SUBSCRIPTION_NAME, CREATE_DATE, UPDATE_DATE) values ('{0}', '{1}', getdate(), getdate())", _subscriptionCode, _subscriptionName)

            _db.ExecuteQueryNoResult(sql)
            _db.Commit()

            result.Add("STATE", "0")
            result.Add("SUBSCRIPTION_CODE", (New Polestar.Security.Cryptography()).Encrypt(_subscriptionCode, _fingerPrint))


        Catch ex As Exception
            _db.Rollback()


            result.Add("STATE", "1")
            result.Add("SUBSCRIPTION_CODE", String.Empty)

        Finally
            _db.Close()

        End Try

        Return result

    End Function

    Public Function remove(fingerPrint As String, subscriptionCode As String) As Boolean
        Dim result As Boolean = False
        Dim _subscriptionCode As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(subscriptionCode)
        Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
        Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
        Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString, True)
        _db.Open()
        Try
            Dim sql As String = String.Format("update SUBSCRIPTION set INVALID=1 , UPDATE_DATE=getDate() where INVALID=0 and  SUBSCRIPTION_CODE='{0}'", _subscriptionCode)

            _db.ExecuteQueryNoResult(sql)
            _db.Commit()
        Catch ex As Exception
            _db.Rollback()

        Finally
            _db.Close()

        End Try
        Return result
    End Function

    Public Function exist(fingerPrint As String, subscriptionCode As String) As Boolean

        Dim _subscriptionCode As String = (New polestar.Security.Cryptography()).Decrypt(subscriptionCode, fingerPrint)

        Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
        Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
        Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString)
        Dim sql As String = String.Format("select * from SUBSCRIPTION where INVALID=0 and  SUBSCRIPTION_CODE='{0}'", _subscriptionCode)
        Dim result As Boolean = False
        _db.Open()
        Dim dt As System.Data.DataTable = _db.DataReader2DataTable(_db.ExecuteQuery(sql))
        If dt.Rows.Count > 0 Then result = True
        _db.Close()

        Return result

    End Function

    Private Function genarateSubscriptionCode() As String

        Const keychars As String = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"

        Dim result As String = String.Empty
        Dim sb As System.Text.StringBuilder
        Dim r As New System.Random

        sb = New System.Text.StringBuilder(5)

        For i As Integer = 0 To 4
            Dim p As Integer = r.Next(keychars.Length)
            sb.Append(keychars(p))
        Next

        result = sb.ToString & "-"

        sb = New System.Text.StringBuilder(5)

        For i As Integer = 0 To 4
            Dim p As Integer = r.Next(keychars.Length)
            sb.Append(keychars(p))
        Next

        result = result + sb.ToString & "-"


        sb = New System.Text.StringBuilder(5)

        For i As Integer = 0 To 4
            Dim p As Integer = r.Next(keychars.Length)
            sb.Append(keychars(p))
        Next

        result = result + sb.ToString & "-"


        sb = New System.Text.StringBuilder(5)

        For i As Integer = 0 To 4
            Dim p As Integer = r.Next(keychars.Length)
            sb.Append(keychars(p))
        Next

        result = result + sb.ToString & "-"

        sb = New System.Text.StringBuilder(5)

        For i As Integer = 0 To 4
            Dim p As Integer = r.Next(keychars.Length)
            sb.Append(keychars(p))
        Next

        result = result + sb.ToString

        Return result

    End Function


End Class
