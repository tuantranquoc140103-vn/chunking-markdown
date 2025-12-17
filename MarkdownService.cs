
using System.ComponentModel;
using System.Net.NetworkInformation;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;


public enum TypeChunk
{
    [Description("Paragraph")]
    Paragraph = 1,
    [Description("Header")]
    Header = 2,
    [Description("Table")]
    Table = 3,
    [Description("All content")]
    AllContent = 4
}

public class SpanChunk
{
    public int Start { get; set; }
    public int End { get; set; }
    public required TypeChunk Type { get; set; }
}

public class ChunkInfo
{
    public TypeChunk Type { get; set; }
    public int TokensCount { get; set; }
}

public class MarkdownService
{
    private readonly TokenCountService _tokenCountService;
    private const int MAX_TOKENS_PER_CHUNK = 8192;
    private const int MAX_DEEP_HEADER = 4;
    private TypeChunk typeChunk = TypeChunk.AllContent;


    // private int countLoop = 0;

    public MarkdownService(TokenCountService tokenCountService)
    {
        _tokenCountService = tokenCountService;
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

    /// <summary>
    /// Logic xử lý tạo content bằng header level
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="blocks"></param>
    /// <param name="headerLevel"></param>
    /// <returns></returns>
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

        // tiền xử lý cứ phải lọc tất cả các header cùng level
        blocks = blocks.Where(x => x.Level == headerLevel).ToList();

        // điều kiện thực hiện là tất cả các header phải cùng level
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

    public void PrintCountHeader(List<KeyValuePair<HeadingBlock, string>> headers, int maxDeep = 3)
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
            var tokenResponse = _tokenCountService.CountAsync(new CountRequest { Text = item.Value, ReturnTokens = false }).Result;
            int lengthWord = tokenResponse.TokenCount;
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

                var result = SplitToNextHeaders(item);

                // Gọi đệ quy cho các nhánh con (nó sẽ tiếp tục vẽ tree)
                PrintCountHeader(result);
            }
        }
    }

    /// <summary>
    /// Lấy nội dung giữa hai header bao gồm cả header parent
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetContentBetweenHeader(HeadingBlock? parent, HeadingBlock? child, string source)
    {
        if (parent is null && child is null)
        {
            throw new ArgumentNullException(nameof(parent), nameof(child));
        }
        if (parent is null && child is not null)
        {
            string res = source.Substring(0, child.Span.Start-1);
            return res;

        }

        if (child is null && parent is not null)
        {
            string res = source.Substring(parent.Span.Start, source.Length - parent.Span.Start - 1);
            return res;
        }



        string contentParent = source.Substring(parent!.Span.Start, parent.Span.Length);
        string contentBetween = source.Substring(parent.Span.End + 1, child!.Span.Start - parent.Span.End - 1);
        if (string.IsNullOrWhiteSpace(contentBetween))
        {
            // Console.WriteLine($"Content parent: {contentParent}");
            return string.Empty;
        }

        return $"{contentParent}{contentBetween}";
    }

    public static List<Block> GetAllBlock(string source)
    {
        var markdownPipline = new MarkdownPipelineBuilder()
                                .UsePipeTables()
                                .UseEmphasisExtras()
                                .Build();
        MarkdownDocument document = Markdown.Parse(source, markdownPipline);
        // List<Block> blocks = new List<Block>();
        // foreach (Block block in document)
        // {
        //     blocks.Add(block);
        //     block.Be
        // }
        // return blocks;

        return document.ToList<Block>();
    }

    /// <summary>
    /// step by step check Token count document
    ///     if <= 8192 --> create chunk is this document
    ///     else --> split to next header
    /// Duyệt --> get all header in document
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public List<KeyValuePair<ChunkInfo, string>> CreateChunkDocument(string source)
    {
        List<KeyValuePair<ChunkInfo, string>> result = new List<KeyValuePair<ChunkInfo, string>>();

        CreateChunks(source, ref result);
        return result;
    }


    /// <summary>
    /// Tạo chunk theo level heder
    /// </summary>
    /// <param name="source"></param>
    /// <param name="chunks"></param>
    /// <param name="headerLevel">level 0 là all content</param>
    private void CreateChunks(string source, ref List<KeyValuePair<ChunkInfo, string>> chunks, int headerLevel = 0)
    {
        // Console.WriteLine($"Loop i: {++countLoop}");

        var tokenResponse = _tokenCountService.CountAsync(new CountRequest { Text = source, ReturnTokens = false }).Result;
        var allBlocks = GetAllBlock(source);


        if (tokenResponse.TokenCount < MAX_TOKENS_PER_CHUNK)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                var chunkInfo = new ChunkInfo()
                {
                    Type = typeChunk,
                    TokensCount = tokenResponse.TokenCount
                };
                chunks.Add(new KeyValuePair<ChunkInfo, string>(chunkInfo, source));
                CreateChunkTable(allBlocks, ref chunks, source);
            }
            return;
        }

        if (headerLevel > MAX_DEEP_HEADER)
        {
            // lúc này không thể chia theo header nữa 
            // bắt buộc phải chia theo regex, có thể là trích xuất bảng trong header hoặc là chia theo listitem được không ?? chỗ này cần xem tài liệu có format như nào
            // nếu như không có format thì thực hiện chia theo regex với mỗi chunk là 2048 tokens
            // --> dùng cách gì đó để gộp tất cả các bảng trong header đó thành 1 chunk nếu như các bảng đó là 1 (Vì bảng dài thì nó sẽ bị trải dài ra nhiều page lúc OCR)

            // thực hiện return ở đây để kết thúc đệ quy
            CreateChunkByRegex(source, ref chunks);
            CreateChunkTable(allBlocks, ref chunks, source);
            Console.WriteLine("Stop, reson: headerLevel > MAX_DEEP_HEADER");
            return;
        }



        var headers = allBlocks.Where(b => b is HeadingBlock h && h.Level == headerLevel).Select(b => (HeadingBlock)b).ToList();
        // Console.WriteLine($"contentHeaders level {headerLevel} count: {headers.Count}");

        if (headers.Count == 0)
        {
            CreateChunks(source, ref chunks, headerLevel + 1);
            return;
        }

        // cái này là không lấy nội dung của header 
        // chỉ lấy từ đầu tới start index header
        // string contentBeforHeader = GetContentBetweenHeader(null, headers[0], source);

        // var tokenResponseBeforHeader = _tokenCountService.CountAsync(new CountRequest { Text = contentBeforHeader, ReturnTokens = false }).Result;
        // if (tokenResponseBeforHeader.TokenCount <= MAX_TOKENS_PER_CHUNK)
        // {
        //     var chunkInfo = new ChunkInfo()
        //     {
        //         Type = TypeChunk.Paragraph,
        //         TokensCount = tokenResponseBeforHeader.TokenCount
        //     };
        //     chunks.Add(new KeyValuePair<ChunkInfo, string>(chunkInfo, contentBeforHeader));
        // }
        // else
        // {
        //     // đoạn này không truyền headerlevel + 1 là vì chảng hạn như 
        //     // ta tách theo header 1 nhưng trước đó là gồm các nội dung nhỏ mà có header 2,3 thì nó sẽ tự
        //     // đệ quy để tăng headerlevel + 1 do token text lớn hơn maxToken per chunk
        //     CreateChunks(contentBeforHeader, ref chunks, headerLevel);
        // }

        // xử lý cho header ở đầu
        if (headers.Count == 1)
        {
            string contentFirstHeader = GetContentBetweenHeader(null, headers[0], source);
            // int index = allBlocks.IndexOf(headers[0]);
            // if(index > 0)
            // {
                
            // }
            CreateChunks(contentFirstHeader, ref chunks, headerLevel + 1);
            string contentLastHeader = GetContentBetweenHeader(headers[0], null, source);
            CreateChunks(contentLastHeader, ref chunks, headerLevel + 1);
            return;
        }
        if (headers.Count > 1)
        {
            var secondHeader = headers[1];
            string contentFirstHeader = GetContentBetweenHeader(null, secondHeader, source);
            CreateChunks(contentFirstHeader, ref chunks, headerLevel + 1);
        }

        for (int i = 1; i < headers.Count; i++)
        {
            if (i == headers.Count - 1)
            {
                var lastHeader = headers.Last();
                string contentLastHeader = GetContentBetweenHeader(lastHeader, null, source);
                CreateChunks(contentLastHeader, ref chunks, headerLevel + 1);
                // Console.WriteLine("Nó có vào đây");
                return;
            }
            var header = headers[i];
            var nextHeader = headers[i + 1];
            string content = GetContentBetweenHeader(header, nextHeader, source);
            CreateChunks(content, ref chunks, headerLevel + 1);
        }

        return;

    }

    private void CreateChunkByRegex(string source, ref List<KeyValuePair<ChunkInfo, string>> chunks)
    {
        // throw new NotImplementedException();
    }


    /// <summary>
    /// Hiện tại phục vụ cho table thôi
    /// </summary>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private KeyValuePair<ChunkInfo, string> GetContentByIndex(string source, int start, int end)
    {
        if (start < 0 || end > source.Length)
        {
            throw new ArgumentException("Index out of range");
        }
        else
        {
            var content = source.Substring(start, end - start + 1);

            var tokenResponse = _tokenCountService.CountAsync(new CountRequest { Text = content, ReturnTokens = false }).Result;
            var chunkInfo = new ChunkInfo()
            {
                Type = TypeChunk.Table,
                TokensCount = tokenResponse.TokenCount
            };
            return new KeyValuePair<ChunkInfo, string>(chunkInfo, content);

        }
    }

    public void CreateChunkTable(List<Block> blocks, ref List<KeyValuePair<ChunkInfo, string>> chunks, string source)
    {
        bool IsTitleTable(Block block)
        {
            return block is ParagraphBlock || block is ListItemBlock || block is HeadingBlock || block is ListBlock;
        }

        List<HtmlBlock> htmlBlocks = blocks.Where(b => b is HtmlBlock h && IsTableBlock(h))
                                            .Select(b =>
                                            {
                                                var html = (HtmlBlock)b;
                                                // Console.WriteLine($"Start index: {html.Span.Start}");
                                                // Console.WriteLine($"End index: {html.Span.End}");
                                                // Console.WriteLine(new string('-', 50));
                                                return html;
                                            }).ToList();
        
        List<Table> tables = blocks.Where(b => b.GetType() == typeof(Table))
                                    .Select(b =>
                                    {
                                        var table = (Table)b;
                                        // Console.WriteLine($"Start index: {table.Span.Start}");
                                        // Console.WriteLine($"End index: {table.Span.End}");
                                        // Console.WriteLine(new string('-', 50));
                                        return table;
                                    }).ToList();

       
        foreach (var html in htmlBlocks)
        {
            try
            {
                int index = blocks.IndexOf(html);
                var contentTable = GetContentByIndex(source, html.Span.Start, html.Span.End);
                // var content = GetContentByIndex(source, blocks[index - 1].Span.End, html.Span.Start - 1);
                if (index > 0)
                {
                    var blockBefore = blocks[index - 1];
                    if (IsTitleTable(blockBefore))
                    {
                        var contentChunk = GetContentByIndex(source, blockBefore.Span.Start, html.Span.End);
                        chunks.Add(contentChunk);
                        continue;
                    }
                }
                chunks.Add(contentTable);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        foreach (var table in tables)
        {
            try
            {
                int index = blocks.IndexOf(table);
                // var content = GetContentByIndex(source, blocks[index - 1].Span.End, table.Span.Start - 1);
                var contentTable = GetContentByIndex(source, table.Span.Start, table.Span.End);
                if (index > 0)
                {
                    var blockBefore = blocks[index - 1];
                    if (IsTitleTable(blockBefore))
                    {
                        var contentChunk = GetContentByIndex(source, blockBefore.Span.Start, table.Span.End);
                        chunks.Add(contentChunk);
                        continue;
                    }

                }
                chunks.Add(contentTable);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


    }


    public void ShowChunks(List<KeyValuePair<ChunkInfo, string>> chunks, int maxChar = 100)
    {
        Console.WriteLine($"Total chunks: {chunks.Count}");

        string underline = new string('-', 50);

        foreach (var (chunkInfo, content) in chunks)
        {
            Console.WriteLine($"{chunkInfo.Type} - {chunkInfo.TokensCount} tokens");
            if (content.Length > maxChar)
            {
                Console.WriteLine(content[..maxChar]);
                Console.WriteLine("|||||||||");
                Console.WriteLine(content[^maxChar..]);
            }
            else
            {
                Console.WriteLine(content);

            }

            Console.WriteLine(underline);
        }
    }
}
