Public Class Authentication

    Private Const connectionString As String = "Server=tcp:t5d2skdq62.database.windows.net,1433;Database=polestarauth;User ID=polestaradmin@t5d2skdq62;Password=1Qaz2Wsx,;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;"



    Public Function getSubscriptionList(fingerPrint As String, email As String) As System.Data.DataTable



        Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
        Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
        Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString)

        Dim sql As String = String.Format("select * from AUTH where INVALID=0 and  AUTH_VALUE='{0}'", (New Polestar.Security.Cryptography).Decrypt(email, fingerPrint))

        _db.Open()
        Dim dt As System.Data.DataTable = _db.DataReader2DataTable(_db.ExecuteQuery(sql))

        If dt.Rows.Count > 0 Then Return Nothing
        _db.Close()




        Return dt


    End Function

    Public ReadOnly Property Item(fingerPrint As String, email As String) As System.Data.DataTable
        Get

            Dim _fingerPrint As String = (New Polestar.Security.Cryptography).getFingerPrint()
            Dim _connectionString As String = (New Polestar.Security.Cryptography).Encrypt(connectionString, _fingerPrint)
            Dim _db As Polestar.cloud.db.SQLServer = New Polestar.cloud.db.SQLServer(_fingerPrint, _connectionString)

            Dim sql As String = String.Format("select SUBSCRIPTION_CODE, EXPIRE_DATE, REF_IDENTIFY from AUTH where INVALID=0 and  AUTH_VALUE='{0}'", (New Polestar.Security.Cryptography).Decrypt(email, fingerPrint))

            _db.Open()
            Dim dt As System.Data.DataTable = _db.DataReader2DataTable(_db.ExecuteQuery(sql))
            _db.Close()

            If dt.Rows.Count <= 0 Then Return Nothing

            dt.Columns.Add("SUBSCRIPTION_NAME", GetType(String))
            Dim dic As New System.Collections.Generic.Dictionary(Of String, String)

            For Each row As System.Data.DataRow In dt.Rows
                dic.Add(row("SUBSCRIPTION_CODE"), String.Empty)
            Next
            Dim subs As New Polestar.cloud.Authentication.Subscription
            dic = subs.getSubscriptionName(dic)

            For Each row As System.Data.DataRow In dt.Rows

                row("SUBSCRIPTION_NAME") = dic(row("SUBSCRIPTION_CODE"))

            Next

            Return dt

        End Get

    End Property

    Public Function SignUp(fingerPrint As String, email As String, password As String, subscriptionCode As String) As Boolean

        Dim result As Boolean = False

        Dim _email As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(email)
        Dim _password As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(password)
        Dim _subscriptionCode As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(subscriptionCode)

        Dim fp As String = (New Polestar.Security.Cryptography).getFingerPrint
        Dim _pimDBConf As Dictionary(Of String, String) = (New Polestar.cloud.pim.PIM).getConnectionString()
        Dim _pimDBConfFingerPrint As String = _pimDBConf.Keys(0)
        Dim _pimDBConnectionString = (New Polestar.Security.Cryptography).Decrypt(_pimDBConf(_pimDBConfFingerPrint), _pimDBConfFingerPrint)

        Dim sql As String
        Dim exp As Exception = Nothing
        Dim contactID As Long = 0


        If (New Polestar.cloud.Authentication.Subscription).exist(fingerPrint, subscriptionCode) = False Then
            Throw New Exception("Invalid Service")
            Return False
        End If

        Dim _authDB As New Polestar.cloud.db.SQLServer(fp, (New Polestar.Security.Cryptography).Encrypt(connectionString, fp), True)
        Dim _pimDB As New Polestar.cloud.db.SQLServer(fp, (New Polestar.Security.Cryptography).Encrypt(_pimDBConnectionString, fp), True)

        contactID = (New Polestar.cloud.pim.Contacts).existEmail(_email)
        'If (New polestar.pim.Contacts).existEmail(_email) Then Throw New Exception("Exist Email Address for Contact")

        Try

            _authDB.Open()
            _pimDB.Open()

            sql = String.Format("select AUTH_ID from AUTH where invalid=0 and AUTH_VALUE='{0}' and SUBSCRIPTION_CODE='{1}'", _email, _subscriptionCode)
            Dim r As System.Data.SqlClient.SqlDataReader = _authDB.ExecuteQuery(sql)
            Dim hasRows As Boolean = r.HasRows
            r.Close()

            If hasRows Then Throw New Exception("Exist Email Address for Authentication")

            If contactID = 0 Then
                Dim contacts As New Polestar.cloud.pim.Contacts
                contacts.add(Polestar.cloud.pim.EnumCotactDevice.EMAIL, Polestar.cloud.pim.EnumContactType.UNDEFINE, _email, _pimDB)
                contactID = contacts.existEmail(_email, _pimDB)

            End If

            sql = String.Format("insert into AUTH ( AUTH_VALUE, PASSWORD, SUBSCRIPTION_CODE, REF_IDENTIFY, EXPIRE_DATE, CREATE_DATE, MODIFY_DATE) values( '{0}','{1}','{2}','{3}', getDate(),getdate(), getdate())", _email, _password, _subscriptionCode, contactID)
            _authDB.ExecuteQueryNoResult(sql)

            _authDB.Commit()
            _pimDB.Commit()

            result = True
        Catch ex As Exception
            _authDB.Rollback()
            _pimDB.Rollback()

            exp = ex
            result = False


        Finally
            _authDB.Dispose()
            _pimDB.Dispose()

        End Try

        If Not IsNothing(exp) Then Throw exp

        Return result

    End Function

    Public Function DefaultSignIn(fingerPrint As String, email As String, password As String) As System.Data.DataTable

        Dim _email As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(email)
        Dim _password As String = (New Polestar.Security.Cryptography(fingerPrint)).Decrypt(password)
        Dim fp As String = (New Polestar.Security.Cryptography).getFingerPrint
        Dim sql As String
        Dim result As New System.Data.DataTable

        Dim _authDB As New Polestar.cloud.db.SQLServer(fp, (New Polestar.Security.Cryptography).Encrypt(connectionString, fp), False)
        _authDB.Open()
        sql = String.Format("select * from AUTH where INVALID=0 and AUTH_VALUE='{0}' and PASSWORD='{1}'", _email, _password)
        Dim dt As System.Data.DataTable = _authDB.DataReader2DataTable(_authDB.ExecuteQuery(sql))
        _authDB.Close()

        If dt.Rows.Count > 0 Then
            '            result = dt.Rows(0).Item(0)
            result = Me.Item(fingerPrint, email)

        Else
            result = Nothing
        End If


        Return result

    End Function

    Public Function SignIn(fingerPrint As String, email As String, password As String, subscriptionCode As String) As Long

        Dim _email As String = (New polestar.Security.Cryptography(fingerPrint)).Decrypt(email)
        Dim _password As String = (New polestar.Security.Cryptography(fingerPrint)).Decrypt(password)
        Dim _subscriptionCode As String = (New polestar.Security.Cryptography(fingerPrint)).Decrypt(subscriptionCode)
        Dim fp As String = (New polestar.Security.Cryptography).getFingerPrint
        Dim sql As String
        Dim result As Long = 0

        Dim _authDB As New polestar.cloud.db.SQLServer(fp, (New polestar.Security.Cryptography).Encrypt(connectionString, fp), False)
        _authDB.Open()
        sql = String.Format("select * from AUTH where INVALID=0 and AUTH_VALUE='{0}' and SUBSCRIPTION_CODE='{1}' and PASSWORD='{2}'", _email, _subscriptionCode, _password)
        Dim dt As System.Data.DataTable = _authDB.DataReader2DataTable(_authDB.ExecuteQuery(sql))

        If dt.Rows.Count > 0 Then
            result = dt.Rows(0).Item(0)
        End If
        _authDB.Close()

        Return result

    End Function

    Public Function getProfile(fingerPrint As String, Identify As String, subscriptionCode As String) As System.Data.DataSet

        Dim result As New System.Data.DataSet
        Dim authDB As New polestar.cloud.db.SQLServer(connectionString)
        Dim contactID As Long = 0


        Dim sql As String
        sql = String.Format(
            "select * from AUTH where invalid=0 and auth_id={0} and SUBSCRIPTION_CODE='{1}'",
            (New polestar.Security.Cryptography).Decrypt(Identify, fingerPrint),
            (New polestar.Security.Cryptography).Decrypt(subscriptionCode, fingerPrint))

        Dim authInfo As System.Data.DataTable = authDB.DataReader2DataTable(authDB.ExecuteQuery(sql), "Account Infomation")
        authDB.Close()

        If authInfo.Rows.Count = 0 Then Throw New Exception("Invalid User")

        contactID = authInfo.Rows(0).Item("REF_IDENTIFY")
        authInfo = (New polestar.cloud.db.SQLServer).cryptDataTable(fingerPrint, authInfo)
        result.Tables.Add(authInfo)

        If contactID = 0 Then Return result


        Return result

    End Function

    Public Function IsEmailFormat(email As String) As Boolean
        If System.Text.RegularExpressions.Regex.IsMatch(email, "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) Then
            Return True
        End If
        Return False
    End Function


End Class


Public Enum EnumAuthResult As Long
    success = 0
    failed = 1

End Enum
