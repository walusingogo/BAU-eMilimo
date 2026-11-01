using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using eMilimo.API.Services;
using eMilimo.Core.Entities;

namespace eMilimo.API.Middleware;

public class ProcessLoggingMiddleware
{
    private readonly RequestDelegate _next;
    public ProcessLoggingMiddleware(RequestDelegate next)=>_next=next;
    public async Task InvokeAsync(HttpContext context,ISystemLogWriter writer)
    {
        if(!context.Request.Path.StartsWithSegments("/api")){await _next(context);return;}
        var timer=Stopwatch.StartNew();Exception? failure=null;var correlation=context.TraceIdentifier;
        try{await _next(context);}catch(Exception ex){failure=ex;context.Response.StatusCode=500;throw;}finally
        {
            timer.Stop();var parts=context.Request.Path.Value?.Split('/',StringSplitOptions.RemoveEmptyEntries)??[];var process=parts.Length>1?parts[1]:"system";var action=DescribeAction(context.Request.Method,parts.Skip(2));
            Guid? userId=Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)??context.User.FindFirstValue("sub"),out var id)?id:null;
            var status=context.Response.StatusCode;var entry=new SystemLog{Level=failure!=null||status>=500?"Error":status>=400?"Warning":"Information",Category="Process",Process=process,Action=action,HttpMethod=context.Request.Method,Path=context.Request.Path+context.Request.QueryString,StatusCode=status,DurationMilliseconds=timer.ElapsedMilliseconds,UserId=userId,MembershipId=context.User.FindFirstValue("membershipId"),UserRole=context.User.FindFirstValue(ClaimTypes.Role),IpAddress=context.Connection.RemoteIpAddress?.ToString(),CorrelationId=correlation,Message=$"{context.Request.Method} {context.Request.Path} completed with status {status}.",Details=JsonSerializer.Serialize(new{endpoint=context.GetEndpoint()?.DisplayName,userAgent=context.Request.Headers.UserAgent.ToString()}),Exception=failure?.ToString()};
            try{await writer.WriteAsync(entry);}catch{ /* Logging must never break the business process. */ }
        }
    }
    private static string DescribeAction(string method,IEnumerable<string> segments){var suffix=string.Join('/',segments);return string.IsNullOrWhiteSpace(suffix)?method switch{"GET"=>"List","POST"=>"Create","PUT"=>"Update","DELETE"=>"Delete",_=>method}:$"{method} {suffix}";}
}
