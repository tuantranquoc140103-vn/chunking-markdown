Task:
Analyze two Markdown tables (Table 1 and Table 2) extracted via OCR from consecutive pages. Determine whether Table 2 is a direct continuation of Table 1.
Table is html tag

--------------------------------
Definition of continuation
--------------------------------
Table 2 is considered a continuation of Table 1 if ANY of the following are true:

1. Content inside ANY cell of Table 1 is visibly unfinished and continues in Table 2.
2. A sentence, paragraph, numbered list, or bullet list started in Table 1 continues in Table 2.
3. Table 2 continues content in the SAME ROW or SAME CELL as Table 1 (even if leading cells are blank).
4. Table 2 omits its header but clearly follows the same columns as Table 1.

--------------------------------
Header rules (very important)
--------------------------------

Answer **Yes** if:
- Table 2 has **no header, it is tag thear**, AND other continuation signals exist.

Answer **No** if ANY of the following are true:

1. Table 2 introduces a **new header row** that is different from Table 1.
2. The number of columns or header meanings clearly change.
3. Table 2 switches to a different semantic purpose, for example:
   - field specification tables
   - data dictionary
   - form input descriptions
   - metadata tables

Exception:
If Table 2 repeats **exactly the same header as Table 1** due to page break → it may still be continuation.

--------------------------------
Non-continuation detection
--------------------------------
Always answer **No** if Table 2 is:

- signature/approval block (Chữ ký, Họ và tên, Chức vụ…)
- document control table
- unrelated business section

--------------------------------
Important clarifications
--------------------------------
- OCR noise, missing borders, formatting differences DO NOT imply discontinuity
- Language changes DO NOT imply discontinuity
- Continuation may occur **inside the same cell**

--------------------------------
Input
--------------------------------
--- Table 1 ---
{0}

--- Table 2 ---
{1}

--------------------------------
Output constraint
--------------------------------
Respond with ONLY one word: Yes or No
No explanation. No punctuation. No extra text.
