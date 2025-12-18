
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

public interface ILlmChatCompletion
{
    Task<ChatCompletionResponse> ChatCompletionAsync(List<ChatMessageRequest> request);
    Task<ChatCompletionResponse> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices);
    Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest) where TModel : class;
}

public abstract class LlmChatCompletionBase<T> : ILlmChatCompletion where T : class, ILlmProviderAdapter
{
    // protected readonly ILlmAdapterFactory _llmAdapterFactory;
    public readonly T _llmProviderAdapter;

    protected LlmChatCompletionBase(ILlmAdapterFactory llmAdapterFactory)
    {
        // _llmAdapterFactory = llmAdapterFactory;
        _llmProviderAdapter = llmAdapterFactory.Create<T>();
    }
    public abstract Task<ChatCompletionResponse> ChatCompletionAsync(List<ChatMessageRequest> request);
    public abstract Task<ChatCompletionResponse> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices);
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <param name="messagesRequest"></param>
    /// <exception cref="InvalidOperationException">description</exception>
    /// <remarks>
    /// <para>
    /// Điều kiện throw InvalidOperationException:
    /// </para>
    /// <list type="bullet">
    /// <item>Phản hồi JSON của API không hợp lệ.</item>
    /// <item>Cấu trúc JSON của phản hồi không khớp hoàn toàn với cấu trúc của kiểu <typeparamref name="TModel"/>.</item>
    /// <item>API Client exception</item>
    /// </list>
    /// Người dùng cần đảm bảo <typeparamref name="TModel"/> được định nghĩa chính xác theo yêu cầu schema của mô hình.
    /// </remarks>
    /// <returns></returns>
    public abstract Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest) where TModel : class;

    protected void MergeFields(JsonObject source, JsonObject extraFields)
    {
        if(source == null || extraFields == null) return;

        foreach (var property in extraFields)
        {
            source[property.Key] = property.Value?.DeepClone();
        }
    }
}


public class ChatCompletionResponse
{
}