
// using models.TokenCount;

using System.Net.Http.Json;

public class TokenCountService
{
    private readonly HttpClient _httpClient;
    private const string CountEndpoint = "/count";
    private const string BatchCountEndpoint = "/batch_count";

    /// <summary>
    /// Khởi tạo TokenCountService.
    /// HttpClient thường được đăng ký là Singleton trong DI và được cấu hình base address.
    /// </summary>
    /// <param name="httpClient">Instance của HttpClient được DI.</param>
    public TokenCountService(HttpClient httpClient)
    {
        _httpClient = httpClient;

    }

    /// <summary>
    /// Đếm token cho một văn bản duy nhất. Tương ứng với endpoint POST /count.
    /// </summary>
    /// <param name="request">Request chứa văn bản và tùy chọn trả về token IDs.</param>
    /// <returns>Response chứa số lượng token và token IDs (nếu được yêu cầu).</returns>
    /// <exception cref="HttpRequestException">Ném ra nếu cuộc gọi API thất bại hoặc trả về mã trạng thái lỗi (non-success status code).</exception>
    public async Task<CountResponse> CountAsync(CountRequest request)
    {
        // Gửi request POST dưới dạng JSON
        var response = await _httpClient.PostAsJsonAsync(CountEndpoint, request);

        // Kiểm tra xem cuộc gọi có thành công không
        if (response.IsSuccessStatusCode)
        {
            // Đọc và deserialize nội dung response
            var result = await response.Content.ReadFromJsonAsync<CountResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("API response content was empty or could not be deserialized.");
            }
            return result;
        }
        else
        {
            // Xử lý lỗi
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API call to {CountEndpoint} failed with status code {response.StatusCode}. Content: {errorContent}");
        }
    }

    /// <summary>
    /// Đếm token cho một danh sách văn bản theo batch. Tương ứng với endpoint POST /batch_count.
    /// </summary>
    /// <param name="request">Request chứa danh sách văn bản và tùy chọn trả về token IDs.</param>
    /// <returns>Response chứa danh sách kết quả đếm token.</returns>
    /// <exception cref="HttpRequestException">Ném ra nếu cuộc gọi API thất bại hoặc trả về mã trạng thái lỗi.</exception>
    public async Task<BatchCountResponse> BatchCountAsync(BatchCountRequest request)
    {
        // Gửi request POST dưới dạng JSON
        var response = await _httpClient.PostAsJsonAsync(BatchCountEndpoint, request);

        // Kiểm tra xem cuộc gọi có thành công không
        if (response.IsSuccessStatusCode)
        {
            // Đọc và deserialize nội dung response
            var result = await response.Content.ReadFromJsonAsync<BatchCountResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("API response content was empty or could not be deserialized.");
            }
            return result;
        }
        else
        {
            // Xử lý lỗi
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API call to {BatchCountEndpoint} failed with status code {response.StatusCode}. Content: {errorContent}");
        }
    }
}
