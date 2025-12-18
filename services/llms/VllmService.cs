


public class VllmService : LlmChatCompletionBase<VllmAdapter>
{
    public VllmService(ILlmAdapterFactory llmAdapterFactory) : base(llmAdapterFactory)
    {
    }

    public override Task<ChatCompletionResponse> ChatCompletionAsync(List<ChatMessageRequest> request)
    {
        throw new NotImplementedException();
    }

    public override Task<ChatCompletionResponse> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices)
    {
        throw new NotImplementedException();
    }

    public override Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest)
    {
        throw new NotImplementedException();
    }
}