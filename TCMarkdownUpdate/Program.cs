using MySqlConnector;
using System.Diagnostics;
using System.Text;

namespace TCMarkdownUpdate
{
    internal class Program
    {
        public static bool fileEdited;
        public struct TableData
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Attributes { get; set; }
            public string Key { get; set; }
            public string Null { get; set; }
            public string Default { get; set; }
            public string Extra { get; set; }
            public string Comment { get; set; }
        }

        public static void GetTableNames(MySqlConnection connection, List<string> tableNames)
        {
            uint counter = 0;
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SHOW TABLES;";
            MySqlDataReader reader;
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                tableNames.Add(reader.GetString(0));
                counter++;
            }

            reader.Close();

            Console.WriteLine($"Loaded {counter} table names from: {connection.Database}");
        }

        public static void GetTableData(MySqlConnection connection, string tableName, Dictionary<string /*tableName*/, List<TableData>/*tableData*/> tableDataStore)
        {
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = $"SHOW FULL FIELDS FROM `{tableName}`;";
            MySqlDataReader reader;
            reader = command.ExecuteReader();

            List<TableData> tableDatas = new List<TableData>();
            while (reader.Read())
            {
                TableData tableData = new TableData();
                tableData.Name = reader.GetString(0);
                tableData.Type = reader.GetString(1).Replace(" unsigned", "").Replace("enum('RELEASED','ARCHIVED')", "enum(<br />'RELEASED',<br />'ARCHIVED')");
                if (reader.GetString(1).Contains("int"))
                {
                    if (reader.GetString(1).Contains("unsigned"))
                        tableData.Attributes = "unsigned";
                    else
                        tableData.Attributes = "signed";
                }
                else
                    tableData.Attributes = "";
                tableData.Null = reader.GetString(3);
                tableData.Key = reader.GetString(4);
                if (reader.GetString(3).Equals("NO"))
                {
                    tableData.Default = reader.IsDBNull(5) ? "" : reader.GetValue(5).Equals(string.Empty) ? "''" : reader.GetString(5);
                }
                else
                {
                    tableData.Default = reader.IsDBNull(5) ? "NULL" : reader.GetValue(5).Equals(string.Empty) ? "''" : reader.GetString(5);
                }
                tableData.Extra = reader.GetString(6);
                tableData.Comment = reader.GetString(8);

                tableDatas.Add(tableData);
            }

            reader.Close();

            tableDataStore.Add(tableName, tableDatas);
        }

        static void ChangeLine(string newText, string fileName, int lineToEdit)
        {
            string[] arrLine = File.ReadAllLines(fileName);

            if (arrLine[lineToEdit - 1].Equals(newText))
            {
                if (!fileEdited)
                    return;
            }

            arrLine[lineToEdit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
            fileEdited = true;
        }

        public static string GetPassword()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                int x = Console.CursorLeft;
                int y = Console.CursorTop;
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                    Console.SetCursorPosition(x - 1, y);
                    Console.Write(" ");
                    Console.SetCursorPosition(x - 1, y);
                }
                else if (key.KeyChar < 32 || key.KeyChar > 126)
                {
                    Trace.WriteLine("Output suppressed: no key char"); //catch non-printable chars, e.g F1, CursorUp and so ...
                }
                else if (key.Key != ConsoleKey.Backspace)
                {
                    input.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return input.ToString();
        }

        static public void Main(string[] args)
        {
            string db = "";
            string version = "";
            while (version.Equals(""))
            {
                Console.WriteLine("Select TrinityCore branch:");
                Console.WriteLine("[1] 335");
                Console.WriteLine("[2] master");
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    input = "3";

                switch (int.Parse(input))
                {
                    case 1:
                        version = "335";
                        Console.WriteLine("Selected branch: 335");
                        break;
                    case 2:
                        version = "master";
                        Console.WriteLine("Selected branch: master");
                        break;
                    default:
                        Console.Clear();
                        Console.WriteLine($"Invalid input!");
                        Console.WriteLine();
                        break;
                }
            }
            while (db.Equals(""))
            {
                Console.Clear();
                Console.WriteLine($"Selected branch: {version}");
                Console.WriteLine();
                Console.WriteLine("Select TrinityCore Database:");
                Console.WriteLine("[1] world");
                Console.WriteLine("[2] auth");
                Console.WriteLine("[3] characters");
                if (version.Equals("master"))
                    Console.WriteLine("[4] hotfixes");

                string? input2 = Console.ReadLine();

                if (string.IsNullOrEmpty(input2))
                    input2 = "5";

                switch (int.Parse(input2))
                {
                    case 1:
                        db = "world";
                        Console.WriteLine("Selected db: world");
                        break;
                    case 2:
                        db = "auth";
                        Console.WriteLine("Selected db: auth");
                        break;
                    case 3:
                        db = "characters";
                        Console.WriteLine("Selected db: characters");
                        break;
                    case 4:
                        if (version.Equals("master"))
                        {
                            db = "hotfixes";
                            Console.WriteLine("Selected db: hotfixes");
                        }
                        else
                            goto default;
                        break;
                    default:
                        Console.Clear();
                        Console.WriteLine($"Invalid input!");
                        Console.WriteLine();
                        break;
                }
            }

            Console.Clear();
            Console.WriteLine($"Selected branch: {version}");
            Console.WriteLine($"Selected database: {db}");
            Console.WriteLine();

            Console.WriteLine("Please enter MySQL server adress (default: 127.0.0.1):");
            var server = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrEmpty(server))
                server = "127.0.0.1";
            Console.Clear();
            Console.WriteLine($"Selected branch: {version}");
            Console.WriteLine($"Selected database: {db}");
            Console.WriteLine($"MySQL Server: {server}");
            Console.WriteLine();

            Console.WriteLine("Please enter MySQL user:");
            string? user = null;
            while (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(user))
                user = Console.ReadLine();

            Console.Clear();
            Console.WriteLine($"Selected branch: {version}");
            Console.WriteLine($"Selected database: {db}");
            Console.WriteLine($"MySQL Server: {server}");
            Console.WriteLine($"MySQL User: {user}");
            Console.WriteLine();

            Console.WriteLine("Please enter MySQL password:");
            string? password = null;
            while (string.IsNullOrWhiteSpace(password) || string.IsNullOrEmpty(password))
                password = GetPassword();

            Console.Clear();
            Console.WriteLine($"Selected branch: {version}");
            Console.WriteLine($"Selected database: {db}");
            Console.WriteLine($"MySQL Server: {server}");
            Console.WriteLine($"MySQL User: {user}");
            Console.WriteLine();

            Console.WriteLine($"Please enter the {db} database name:");
            string? database = null;
            while (string.IsNullOrWhiteSpace(database) || string.IsNullOrEmpty(database))
                database = Console.ReadLine();
            Console.Clear();
            Console.WriteLine($"Selected branch: {version}");
            Console.WriteLine($"Selected database: {db}");
            Console.WriteLine($"MySQL Server: {server}");
            Console.WriteLine($"MySQL User: {user}");
            Console.WriteLine($"MySQL database: {database}");
            Console.WriteLine();


            Dictionary<string /*tableName*/, List<TableData>/*tableData*/> tableDataStore = new Dictionary<string , List<TableData>>();
            List<string> tableNames = new List<string>();

            using (MySqlConnection connection = new MySqlConnection($"SERVER={server}; DATABASE={database}; UID={user}; PASSWORD={password};"))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connecting to MySQL...");

                    GetTableNames(connection, tableNames);

                    foreach (var tableName in tableNames)
                        GetTableData(connection, tableName, tableDataStore);

                    connection.Close();
                }
                catch (MySqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

            string currentDictionary = Directory.GetCurrentDirectory();

            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                string tableNameBack;
                string tableNameForward;
                if (tableNames.Count == 1)
                {
                    tableNameBack = "";
                    tableNameForward = "";
                }
                else if (i == 0)
                {
                    tableNameBack = tableNames[tableNames.Count - 1];
                    tableNameForward = tableNames[i + 1];
                }
                else if (i == tableNames.Count - 1)
                {
                    tableNameForward = tableNames[0];
                    tableNameBack = tableNames[i - 1];
                }
                else
                {
                    tableNameBack = tableNames[i - 1];
                    tableNameForward = tableNames[i + 1];
                }

                string path = Path.Combine(currentDictionary + "//" + tableName + ".md");
                if (!File.Exists(path))
                {
                    using (StreamWriter outputFile = new StreamWriter(path, true))
                    {
                        // HEADER
                        outputFile.WriteLine("---");
                        outputFile.WriteLine("title: {0}", tableName);
                        outputFile.WriteLine("description: ");
                        outputFile.WriteLine("published: true");
                        outputFile.WriteLine("date: {0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                        outputFile.WriteLine("tags: database, {0}, {1}", version, db);
                        outputFile.WriteLine("editor: markdown");
                        outputFile.WriteLine("dateCreated: {0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                        outputFile.WriteLine("---");
                        outputFile.WriteLine();

                        // BUTTONS
                        outputFile.WriteLine("<a href=\"https://trinitycore.info/en/database/{0}/{1}/{2}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-arrow-left theme--light\"></i><span>Back to '{2}'</span></span></a>" +
                            "&nbsp;&nbsp;&nbsp;" +
                            "<a href=\"https://trinitycore.info/en/database/{0}/{1}/home\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-home-outline theme--light\"></i><span>Return to {1}</span></span></a>" +
                            "&nbsp;&nbsp;&nbsp;" +
                            "<a href=\"https://trinitycore.info/en/database/{0}/{1}/{3}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><span>Go to '{3}'</span><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--right mdi mdi-arrow-right theme--light\"></i></span></a>", version, db, tableNameBack, tableNameForward);
                        outputFile.WriteLine();

                        // STRUCTURE
                        outputFile.WriteLine("## Structure");
                        outputFile.WriteLine();
                        outputFile.WriteLine("| Field | Type | Attributes | Key | Null | Default | Extra | Comment |");
                        outputFile.WriteLine("| --- | --- | --- | :---: | :---: | --- | --- | --- |");

                        List<TableData>? tableDatas;
                        List<string> columnNames = new List<string>();
                        if (tableDataStore.TryGetValue(tableName, out tableDatas))
                        {
                            foreach (TableData tableData in tableDatas)
                            {
                                columnNames.Add(tableData.Name);
                                if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                    outputFile.WriteLine("| [{0}](#{1}-alt) | {2} | {3} | {4} | {5} | {6} | {7} | {8} |", tableData.Name, tableData.Name.ToLower(), tableData.Type, tableData.Attributes, tableData.Key, tableData.Null, tableData.Default, tableData.Extra, tableData.Comment);
                                else
                                    outputFile.WriteLine("| [{0}](#{1}) | {2} | {3} | {4} | {5} | {6} | {7} | {8} |", tableData.Name, tableData.Name.ToLower(), tableData.Type, tableData.Attributes, tableData.Key, tableData.Null, tableData.Default, tableData.Extra, tableData.Comment);
                            }
                        }
                        outputFile.WriteLine("&nbsp;");

                        // DESCRIPTION
                        outputFile.WriteLine("## Description of fields");
                        outputFile.WriteLine();

                        foreach (string columnName in columnNames)
                        {
                            if (columnName.Equals("VerifiedBuild"))
                            {
                                outputFile.WriteLine("### {0}", columnName);
                                outputFile.WriteLine("This field is used by the TrinityDB Team to determine whether a template has been verified from WDB files.");
                                outputFile.WriteLine();
                                outputFile.WriteLine("If value is 0 then it has not been parsed yet.");
                                outputFile.WriteLine();
                                outputFile.WriteLine("If value is above 0 then it has been parsed with WDB files from that specific client build.");
                                outputFile.WriteLine();
                                outputFile.WriteLine("If value is -1 then it is just a place holder until proper data are found on WDBs.");
                                outputFile.WriteLine();
                                outputFile.WriteLine("If value is -Client Build then it was parsed with WDB files from that specific client build and manually edited later for some special necessity.");
                                outputFile.WriteLine();
                                outputFile.WriteLine("&nbsp;");
                                outputFile.WriteLine();
                            }
                            else
                            {
                                if (columnName.ToLower().Equals("id") || columnName.ToLower().Equals("name") || columnName.ToLower().Equals("action"))
                                    outputFile.WriteLine("### {0} <!-- {{#{1}-alt}} -->", columnName, columnName.ToLower());
                                else
                                    outputFile.WriteLine("### {0}", columnName);
                                outputFile.WriteLine("*- no description -*");
                                outputFile.WriteLine("&nbsp;");
                                outputFile.WriteLine();
                            }
                        }

                        // BUTTONS
                        outputFile.WriteLine("<a href=\"https://trinitycore.info/en/database/{0}/{1}/{2}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-arrow-left theme--light\"></i><span>Back to '{2}'</span></span></a>" +
                                                    "&nbsp;&nbsp;&nbsp;" +
                                                    "<a href=\"https://trinitycore.info/en/database/{0}/{1}/home\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-home-outline theme--light\"></i><span>Return to {1}</span></span></a>" +
                                                    "&nbsp;&nbsp;&nbsp;" +
                                                    "<a href=\"https://trinitycore.info/en/database/{0}/{1}/{3}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><span>Go to '{3}'</span><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--right mdi mdi-arrow-right theme--light\"></i></span></a>", version, db, tableNameBack, tableNameForward);
                    }
                }
                else
                {
                    fileEdited = false;

                    int lineCounter = 1;
                    int structureSectionLine = 1;
                    int structureSectionLength = 0;
                    bool hasSourceInSniff = false;
                    foreach (string line in File.ReadLines(path))
                    {
                        if (line.Contains("## Structure"))
                            structureSectionLine = lineCounter;

                        if (line.Contains("Source in sniff"))
                            hasSourceInSniff = true;

                        if (structureSectionLine >= 2)
                        {
                            if (line.Contains("&nbsp;"))
                            {
                                structureSectionLength = lineCounter - structureSectionLine + 1;
                                break;
                            }
                        }

                        lineCounter++;
                    }

                    int fieldCount = structureSectionLength - 5;
                    int firstField = structureSectionLine + 4;
                    int lastField = structureSectionLine + structureSectionLength - 2;
                    string[] arrLine = File.ReadAllLines(path);

                    // Store source in sniff
                    Dictionary<string, string> sourceInSniffDescription = new Dictionary<string, string>();
                    if (hasSourceInSniff)
                    {
                        for (int line = 0; line < arrLine.Length; line++)
                        {
                            if (arrLine[line].Contains("Source in sniff"))
                            {
                                for (int k = line + 2; k < line + 2 + fieldCount; k++)
                                {
                                    string field = arrLine[k].Split("|")[1].Split('[', ']')[1];
                                    string sourceInSniff = arrLine[k].Split("|")[9];

                                    int sourceLength = sourceInSniff.Length;
                                    if (sourceInSniff.Substring(sourceLength - 1, 1) == " ")
                                        sourceInSniff = sourceInSniff.Remove(sourceLength - 1, 1);

                                    if (sourceInSniff.Substring(0, 1) == " ")
                                        sourceInSniff = sourceInSniff.Remove(0, 1);

                                    sourceInSniffDescription.Add(field, sourceInSniff);
                                }
                            }
                        }
                    }

                    // Store present descriptions
                    Dictionary<string, List<string>> fileColumnDescription = new Dictionary<string, List<string>>();
                    for (int line = 0; line < arrLine.Length; line++)
                    {
                        if (arrLine[line].StartsWith("### "))
                        {
                            List<string> description = new List<string>();
                            for (int k = line + 1; k < arrLine.Length; k++)
                            {
                                if (!arrLine[k].StartsWith("### "))
                                {
                                    if (arrLine[k].Contains("<a href=\"https://trinitycore.info"))
                                        break;

                                    description.Add(arrLine[k]);
                                }
                                else
                                    break;
                            }
                            Console.WriteLine("Storing table {0} description of: {1}", tableName, arrLine[line].Replace("### ", "").Replace(" <!-- {#id-alt} -->", "").Replace(" <!-- {#name-alt} -->", "").Replace(" <!-- {#action-alt} -->", ""));
                            fileColumnDescription.Add(arrLine[line].Replace("### ", "").Replace(" <!-- {#id-alt} -->", "").Replace(" <!-- {#name-alt} -->", "").Replace(" <!-- {#action-alt} -->", ""), description);
                        }
                    }

                    List<TableData>? tableDatas;
                    if (tableDataStore.TryGetValue(tableName, out tableDatas))
                    {
                        // Update structure simple
                        if (fieldCount == tableDatas.Count)
                        {
                            int line = firstField;
                            foreach (TableData tableData in tableDatas)
                            {
                                if (hasSourceInSniff)
                                {
                                    string? sourceInSniff;
                                    if (sourceInSniffDescription.TryGetValue(tableData.Name, out sourceInSniff))
                                    {
                                        if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                            ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} | {sourceInSniff} |", path, line);
                                        else
                                            ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} | {sourceInSniff} |", path, line);
                                    }
                                    else
                                    {
                                        if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                            ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |  |", path, line);
                                        else
                                            ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |  |", path, line);
                                    }
                                }
                                else
                                {
                                    if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                        ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |", path, line);
                                    else
                                        ChangeLine($"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |", path, line);
                                }
                                line++;
                            }
                        }
                        else
                        {
                            // Update structure
                            string[] newArrLine = new string[arrLine.Length + tableDatas.Count - fieldCount];
                            for (int j = 0; j < arrLine.Length; j++)
                            {
                                if (j < structureSectionLine - 1)
                                {
                                    newArrLine[j] = arrLine[j];
                                    continue;
                                }

                                if (j == structureSectionLine - 1)
                                {
                                    newArrLine[j] = "## Structure";
                                    newArrLine[j + 1] = string.Empty;
                                    if (hasSourceInSniff)
                                    {
                                        newArrLine[j + 2] = "| Field | Type | Attributes | Key | Null | Default | Extra | Comment | Source in sniff |";
                                        newArrLine[j + 3] = "| --- | --- | --- | :---: | :---: | --- | --- | --- | --- |";
                                    }
                                    else
                                    {
                                        newArrLine[j + 2] = "| Field | Type | Attributes | Key | Null | Default | Extra | Comment |";
                                        newArrLine[j + 3] = "| --- | --- | --- | :---: | :---: | --- | --- | --- |";
                                    }

                                    int counter = 1;
                                    foreach (TableData tableData in tableDatas)
                                    {
                                        if (hasSourceInSniff)
                                        {
                                            string? sourceInSniff;
                                            if (sourceInSniffDescription.TryGetValue(tableData.Name, out sourceInSniff))
                                            {
                                                if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                                    newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} | {sourceInSniff} |";
                                                else
                                                    newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} | {sourceInSniff} |";
                                            }
                                            else
                                            {
                                                if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                                    newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |  |";
                                                else
                                                    newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |  |";
                                            }
                                        }
                                        else
                                        {
                                            if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                                newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}-alt) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |";
                                            else
                                                newArrLine[j + 3 + counter] = $"| [{tableData.Name}](#{tableData.Name.ToLower()}) | {tableData.Type} | {tableData.Attributes} | {tableData.Key} | {tableData.Null} | {tableData.Default} | {tableData.Extra} | {tableData.Comment} |";
                                        }
                                        counter++;
                                    }
                                    newArrLine[j + 3 + counter] = "&nbsp;";

                                    j = lastField + 1;
                                }

                                newArrLine[j + tableDatas.Count - fieldCount] = arrLine[j];
                                File.WriteAllLines(path, newArrLine);
                            }
                            fileEdited = true;
                        }

                        // reread, because stuff changed
                        arrLine = File.ReadAllLines(path);

                        int newLineCounter = 1;
                        int descriptionSectionLine = 1;
                        foreach (string line in File.ReadLines(path))
                        {
                            if (line.Contains("## Description of fields"))
                                descriptionSectionLine = newLineCounter;

                            newLineCounter++;
                        }

                        // Update descriptions
                        if (fileEdited)
                        {
                            List<string> newLines = new List<string>();
                            for (int j = 0; j < arrLine.Length; j++)
                            {
                                // Copy everything until description section is reached
                                if (j < descriptionSectionLine - 1)
                                {
                                    newLines.Add(arrLine[j]);
                                    continue;
                                }

                                // Now work with the descriptions
                                if (j == descriptionSectionLine - 1)
                                {
                                    newLines.Add("## Description of fields");
                                    newLines.Add(string.Empty);

                                    foreach (TableData tableData in tableDatas)
                                    {
                                        if (tableData.Name.ToLower().Equals("id") || tableData.Name.ToLower().Equals("name") || tableData.Name.ToLower().Equals("action"))
                                            newLines.Add($"### {tableData.Name} <!-- {{#{tableData.Name.ToLower()}-alt}} -->");
                                        else
                                            newLines.Add($"### {tableData.Name}");
                                        List<string>? descriptions;
                                        if (fileColumnDescription.TryGetValue(tableData.Name, out descriptions))
                                        {
                                            foreach (string desc in descriptions)
                                            {
                                                newLines.Add(desc);
                                            }
                                        }
                                        else
                                        {
                                            newLines.Add("*- no description -*");
                                            newLines.Add("&nbsp;");
                                            newLines.Add(string.Empty);
                                        }
                                    }
                                }

                                if (!arrLine[j].Contains("<a href=\"https://trinitycore.info"))
                                    continue;

                                newLines.Add(arrLine[j]);

                                File.WriteAllLines(path, newLines);
                            }
                        }
                    }

                    // reread, because stuff changed
                    arrLine = File.ReadAllLines(path);

                    for (int j = 0; j < arrLine.Length; j++)
                    {
                        if (arrLine[j].Contains("<a href=\"https://trinitycore.info/"))
                        {
                            ChangeLine($"<a href=\"https://trinitycore.info/en/database/{version}/{db}/{tableNameBack}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-arrow-left theme--light\"></i><span>Back to '{tableNameBack}'</span></span></a>" +
                                "&nbsp;&nbsp;&nbsp;" +
                                $"<a href=\"https://trinitycore.info/en/database/{version}/{db}/home\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--left mdi mdi-home-outline theme--light\"></i><span>Return to {db}</span></span></a>" +
                                "&nbsp;&nbsp;&nbsp;" +
                                $"<a href=\"https://trinitycore.info/en/database/{version}/{db}/{tableNameForward}\" class=\"mt-5 v-btn v-btn--depressed v-btn--flat v-btn--outlined theme--light v-size--default darkblue--text text--lighten-3\"><span class=\"v-btn__content\"><span>Go to '{tableNameForward}'</span><i aria-hidden=\"true\" class=\"v-icon notranslate v-icon--right mdi mdi-arrow-right theme--light\"></i></span></a>", path, j + 1);
                        }
                    }

                    if (fileEdited)
                    {
                        // Line 5 edit date
                        ChangeLine($"date: {DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}", path, 5);
                    }
                }
            }

            string homePath = Path.Combine(currentDictionary + "//home.md");

            using (StreamWriter outputFile = new StreamWriter(homePath, false))
            {
                // HEADER
                outputFile.WriteLine("---");
                outputFile.WriteLine($"title: {char.ToUpper(db[0]) + db.Substring(1)}");
                outputFile.WriteLine("description: ");
                outputFile.WriteLine("published: true");
                outputFile.WriteLine("date: {0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                outputFile.WriteLine($"tags: database, {version}, {db}");
                outputFile.WriteLine("editor: markdown");
                outputFile.WriteLine("dateCreated: 2021-08-30T06:00:00.000Z");
                outputFile.WriteLine("---");
                outputFile.WriteLine();

                string currentLetter = "";
                for (int i = 0; i < tableNames.Count; i++)
                {
                    string letter = tableNames[i].Substring(0, 1).ToLower();
                    if (currentLetter.Equals(""))
                    {
                        outputFile.WriteLine("## " + letter);
                        currentLetter = letter;
                    }

                    if (letter.Equals(currentLetter))
                    {
                        outputFile.WriteLine($"- [{tableNames[i]}](/database/{version}/{db}/{tableNames[i]})");
                    }
                    else
                    {
                        outputFile.WriteLine("{.links-list}");
                        outputFile.WriteLine("## " + letter);
                        outputFile.WriteLine($"- [{tableNames[i]}](/database/{version}/{db}/{tableNames[i]})");
                        currentLetter = letter;
                    }
                }
                outputFile.WriteLine("{.links-list}");
            }

            List<string> files = Directory.GetFiles(currentDictionary, "*.md").ToList();
            foreach (string file in files)
            {
                if (Path.GetFileName(file).Equals("home.md"))
                    continue;

                if (tableNames.Contains(Path.GetFileNameWithoutExtension(file)))
                    continue;
                else
                    File.Delete(file);
            }

            Console.WriteLine("Done.");
        }
    }
}