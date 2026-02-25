All service methods must return `ResponseInfo` or `ResponseInfo<T>`. Follow this pattern:

```csharp
public async Task<ResponseInfo<T>> DoSomethingAsync(...)
{
    var response = new ResponseInfo<T>();
    try
    {
        // ... logic ...
        return response.Success(data);
    }
    catch (Exception e)
    {
        this.LogError(e);
    }
    return response.Fail();
}
```

For simple success/fail without data, use `ResponseInfo.SuccessResult()` / `ResponseInfo.FailedResult("message")`.
