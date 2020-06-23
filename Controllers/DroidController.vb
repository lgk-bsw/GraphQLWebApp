Imports System.Net
Imports System.Net.Http
Imports System.Threading.Tasks
Imports System.Web.Http
Imports GraphQL
Imports GraphQL.Http
Imports GraphQL.Instrumentation
'Imports GraphQL.NewtonsoftJson
Imports GraphQL.Types
Imports GraphQL.Validation.Complexity

Public Class Droid
    Public Property Id As String
    Public Property Name As String
End Class

Public Class DroidType
    Inherits ObjectGraphType(Of Droid)

    Public Sub New()
        Field(Function(x) x.Id).Description("The Id of the Droid")
        Field(Function(x) x.Name).Description("The name of the Droid")
    End Sub
End Class

Public Class StarWarsQuery
    Inherits ObjectGraphType

    Public Sub New()
        Field(Of DroidType)("hero", resolve:=Function(context)
                                                 Return New Droid() With {.Id = "1", .Name = "R2-D2"}
                                             End Function)
    End Sub
End Class

Public Class GraphQLQuery
    Public Property OperationName As String
    Public Property Query As String
    Public Property Variables As Newtonsoft.Json.Linq.JObject
End Class

Namespace Controllers
    Public Class DroidController
        Inherits ApiController

        <HttpPost>
        Public Async Function PostAsync(request As HttpRequestMessage, query As GraphQLQuery) As Task(Of HttpResponseMessage)
            Dim _schema As New Schema() With {.Query = New StarWarsQuery()}

            Dim inputs = query.Variables.ToInputs()
            Dim queryToExecute = query.Query

            Dim httpResult As HttpStatusCode
            Dim json As String = String.Empty

            Try
                json = Await _schema.ExecuteAsync(Function(__)
                                                      __.Schema = _schema
                                                      __.Query = queryToExecute
                                                      __.OperationName = query.OperationName
                                                      __.Inputs = inputs
                                                      __.ComplexityConfiguration = New ComplexityConfiguration With {
                                                                  .MaxDepth = 15
                                                              }
                                                      __.FieldMiddleware.Use(Of InstrumentFieldsMiddleware)()
                                                  End Function).ConfigureAwait(False)
                httpResult = HttpStatusCode.OK
            Catch ex As Exception
                httpResult = HttpStatusCode.BadRequest
            End Try

            Dim response = request.CreateResponse(httpResult)
            response.Content = New StringContent(json, Encoding.UTF8, "application/json")
            Return response
        End Function
    End Class
End Namespace