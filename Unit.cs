using System.Net.NetworkInformation;
using System.Text;
using Markdig;
using Markdig.Syntax;
using SharpToken;

public class Unit
{
    public class MarkdownHelper
    {

        /// <summary>
        /// chỉ lấy được source text của block, không lấy được content ở giữa của các header
        /// </summary>
        /// <param name="source"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public static string ExtractRawText(string source, Block block)
        {
            if (block.Span.Start < 0 || block.Span.End > source.Length)
                return string.Empty;

            return source.Substring(block.Span.Start, block.Span.Length);
        }

        public static bool IsTableBlock(HtmlBlock block)
        {
            if (block is null)
            {
                return false;
            }

            string content = block.Lines.ToString().TrimStart();
            return content.StartsWith("<table");
        }

        
        public static List<KeyValuePair<HeadingBlock, string>> CreateContentBelongtoHeader(string source,
                                                                List<HeadingBlock> blocks,
                                                                int headerLevel = 1)
        {
            string GetContentIfListHeaderIs1(HeadingBlock block)
            {
                string content = source.Substring(block.Span.Start, source.Length - block.Span.Start);

                return content;
            }
                     
            List<KeyValuePair<HeadingBlock, string>> result = new List<KeyValuePair<HeadingBlock, string>>();
            if (blocks.Count == 0)
            {
                return result;
            }


            if (blocks.Count < 2 && blocks[0].Level == headerLevel)
            {
                result.Add(new KeyValuePair<HeadingBlock, string>(blocks[0], GetContentIfListHeaderIs1(blocks[0])));
                return result;
            }
            // Console.WriteLine($"blocks index 3: {blocks[3].Level}");
            if (blocks.Count > 1)
            {
                int count = blocks.Count;
                for (int i = 0; i < count; i++)
                {
                    // Console.WriteLine($"tmp level at index {i}: {blocks[i].Level}");

                    if (i == count - 1 && blocks[i].Level == headerLevel)
                    {
                        result.Add(new KeyValuePair<HeadingBlock, string>(blocks[i], GetContentIfListHeaderIs1(blocks[i])));
                        break;
                    }
                    var tmp = blocks[i];
                    if (tmp.Level == headerLevel)
                    {

                        int j = i + 1;
                        while (j < count)
                        {
                            if (blocks[j].Level <= headerLevel)
                            {
                                // index start = 0
                                string content = source.Substring(tmp.Span.Start, blocks[j].Span.Start - tmp.Span.Start);
                                result.Add(new KeyValuePair<HeadingBlock, string>(tmp, content));
                                break;
                            }
                            j++;
                        }

                        if (j == count)
                        {
                            result.Add(new KeyValuePair<HeadingBlock, string>(tmp, GetContentIfListHeaderIs1(tmp)));
                            break;
                        }
                        i = j - 1;
                    }

                }

            }

            return result;
        }

        /// <summary>
            /// Thực hiện chia nhỏ nội dung của header hiện tại tới các level heder + 1 <br/>
            /// ví dụ nội dung header 1 lớn --> thực hiện chunk tạo các nội dung thuộc header 2 trong nó <br/>
            /// Hàm không thực hiện lấy nội dung ở giữa header parent và header child <br/>
            ///     ví dụ content có dạng như sau: <br/>
            ///     # header 1 <br/>
            ///     content here... <br/>
            ///     ## header 2 <br/>
            /// ---> hàm không tạo chunk chỗ content here... <br/>
            /// Việc tạo content here ... phải được tạo từ lúc trích xuất tất cả các header trong tài liệu gốc
        /// </summary>
        /// <param name="headerAndContent"></param>
        /// <returns></returns>
        public static List<KeyValuePair<HeadingBlock, string>> SplitToNextHeaders(KeyValuePair<HeadingBlock, string> headerAndContent)
        {
            string content = headerAndContent.Value;
            var result = new List<KeyValuePair<HeadingBlock, string>>();

            var pipelineMarkdown = new MarkdownPipelineBuilder()
                            .UsePipeTables()
                            .UseEmphasisExtras()
                            .Build();

            MarkdownDocument document = Markdown.Parse(content, pipelineMarkdown);
            List<HeadingBlock> headers = new List<HeadingBlock>();

            foreach (Block block in document)
            {
                if (block is HeadingBlock headingBlock && headingBlock.Level - headerAndContent.Key.Level == 1)
                {
                    headers.Add(headingBlock);
                    // Console.WriteLine($"Header level: {headingBlock.Level}");
                }
            }

            return CreateContentBelongtoHeader(content, headers, headerAndContent.Key.Level + 1);
        }

        public static void PrintCountHeader(List<KeyValuePair<HeadingBlock, string>> headers, int maxDeep = 3)
        {
            if (headers.Count == 0)
            {
                return;
            }

            // Lấy cấp độ của header
            int currentLevel = headers[0].Key.Level;

            // In tổng số lượng
            Console.WriteLine($"contentHeaders level {currentLevel} count: {headers.Count}");

            for (int i = 0; i < headers.Count; i++)
            {
                var item = headers[i];

                // 1. Xác định tiền tố Tree
                string prefix = "";

                // Tạo thụt lùi cho các cấp trên (không làm việc ở đây, nhưng là ý tưởng)
                // Thay vì tạo thụt lùi cho cấp độ, ta chỉ quan tâm đến cấp độ hiện tại

                // Xác định ký tự nhánh: Dùng '└── ' cho phần tử cuối cùng, '├── ' cho các phần tử khác
                string branch = (i == headers.Count - 1) ? "└── " : "├── ";

                // Xây dựng tiền tố (có thể kết hợp với các cấp độ cha nếu cần vẽ cây hoàn chỉnh)
                // Hiện tại, ta chỉ thụt lùi dựa trên Level và gắn nhánh

                // THAY THẾ TẠM THỜI:
                // Ta sẽ dùng level để tạo thụt lùi (giả sử mỗi cấp độ thụt lùi 4 khoảng trắng)
                string indent = new string(' ', (currentLevel - 1) * 4);
                prefix = indent + branch;

                // int lengthWord = item.Value.Split(' ').Length;
                int lengthWord = TokenHelper.CountTokens(item.Value);
                // int lengthWord = CountWords(item.Value);   

                
                if (item.Value.Length > 20)
                    Console.WriteLine($"{prefix}Count words of header {currentLevel} at index {i}: {lengthWord} --- content: {item.Value[..20]}");
                else
                    Console.WriteLine($"{prefix}Count words of header {currentLevel} at index {i}: {lengthWord} --- content: {item.Value}");
                // Console.Write($" ");

                if (lengthWord > 8192 && currentLevel <= maxDeep)
                {
                    // SỬ DỤNG PREFIX MỚI
                    Console.WriteLine($"{prefix}   Too long. next chunk to header {currentLevel + 1}");
                    Console.WriteLine($"{prefix}   {new string('-', 20)}");

                    var result = Unit.MarkdownHelper.SplitToNextHeaders(item);

                    // Gọi đệ quy cho các nhánh con (nó sẽ tiếp tục vẽ tree)
                    PrintCountHeader(result);
                }
            }
        }

        public static int CountWords(string text)
        {
            // 1. Kiểm tra chuỗi rỗng hoặc null
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            // 2. Định nghĩa các ký tự phân cách (thường là khoảng trắng, tab, ngắt dòng)
            char[] delimiters = new char[] { ' ', '\t', '\n', '\r' };

            // 3. Tách chuỗi thành mảng các từ
            // StringSplitOptions.RemoveEmptyEntries đảm bảo loại bỏ các khoảng trắng kép, đầu/cuối
            string[] words = text.Split(
                delimiters,
                StringSplitOptions.RemoveEmptyEntries
            );

            // 4. Trả về số lượng phần tử trong mảng (số lượng từ)
            return words.Length;

            // Hoặc cách ngắn gọn hơn nếu không muốn tạo biến trung gian:
            /*
            return text.Split(
                new char[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries
            ).Length;
            */
        }

        public static string GetContentBetweenHeader(HeadingBlock? parent, HeadingBlock child, string source)
        {
            if(parent is null && child is null)
            {
                throw new ArgumentNullException(nameof(parent), nameof(child));
            }
            if(parent is null)
            {
                string res = source.Substring(0, child.Span.Start);
                Console.WriteLine($"Content parent: {res[..20]}");
                return res;
                
            }
            string contentParent = source.Substring(parent.Span.Start, parent.Span.Length);
            string contentBetween = source.Substring(parent.Span.End + 1, child.Span.Start - parent.Span.End -1);
            if (string.IsNullOrWhiteSpace(contentBetween))
            {
                // Console.WriteLine($"Content parent: {contentParent}");
                return string.Empty;
            }
            else
            {
                Console.WriteLine($"Content parent: {contentParent}");
                if(contentBetween.Length > 50)
                {
                    Console.WriteLine($"Content between: {contentBetween[..50]}");
                }
                else
                {
                    Console.WriteLine($"Content between: {contentBetween}");
                }
            }
            return $"{contentParent}{Environment.NewLine}{contentBetween}";
        }

    }

    public static class TokenHelper
    {
        // Encoding phổ biến nhất cho các mô hình GPT-4, GPT-3.5-Turbo
        private const string DefaultEncodingName = "cl100k_base";

        // Biến static để lưu trữ Encoding sau khi khởi tạo lần đầu
        private static readonly GptEncoding Encoding;

        // Khối static constructor để khởi tạo Encoding một lần duy nhất 
        // (giúp tối ưu hóa hiệu suất)
        static TokenHelper()
        {
            // Lấy và lưu trữ bảng mã hóa. Việc này chỉ xảy ra một lần.
            Encoding = GptEncoding.GetEncoding(DefaultEncodingName);
        }

        /// <summary>
        /// Tính số lượng token chính xác của một chuỗi dựa trên bảng mã hóa của OpenAI (cl100k_base).
        /// </summary>
        /// <param name="text">Chuỗi văn bản cần tính token.</param>
        /// <returns>Số lượng token.</returns>
        public static int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            // Dùng phương thức Encode() để nhận List<int> (danh sách các ID token)
            var tokens = Encoding.Encode(text);

            // Trả về số lượng token
            return tokens.Count;
        }
    }
}
