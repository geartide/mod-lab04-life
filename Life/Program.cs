﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Collections;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive { get; set; }
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext { get; set; }
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class ProgramSettings {
        public int Width { get; set; }
        public int Height { get; set; }
        public int cellSize { get; set; }
        public double liveDensity { get; set; }
        public int stepsNeeded { get; set; }

        private ProgramSettings()
        {
            this.Width = 0;
            this.Height = 0;
            this.cellSize = 0;
            this.liveDensity = 0;
            this.stepsNeeded = 0;
        }

        public ProgramSettings(int Width, int Height, int cellSize, double liveDensity, int stepsNeeded) 
        {
            this.Width = Width;
            this.Height = Height;
            this.cellSize = cellSize;
            this.liveDensity = liveDensity;
            this.stepsNeeded = stepsNeeded;
        }

        public ProgramSettings(ProgramSettings ps) {
            this.Width = ps.Width;
            this.Height = ps.Height;
            this.cellSize = ps.cellSize;
            this.liveDensity = ps.liveDensity;
            this.stepsNeeded = ps.stepsNeeded;
        }

        public ProgramSettings(string filename) : this(JsonSerializer.Deserialize<ProgramSettings>(File.ReadAllText(filename)))
        { }

        public static void WriteToFile(string filename, ProgramSettings ps) {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(ps, options);
            File.WriteAllText(filename, jsonString);
        }

        public static ProgramSettings ReadFromFile(string filename)
        {
            return new ProgramSettings(filename);
        }
    }

    public enum PatternState
    {
        Any,
        Alive,
        Dead
    }

    public struct PatternCell {

        public int x;
        public int y;

        public PatternState ps;

        public PatternCell(int x, int y, PatternState ps = PatternState.Any) {
            this.x = x;
            this.y = y;
            this.ps = ps;
        }
    }

    public class Pattern : IEnumerable<PatternCell> {

        /* Вид файла с паттерном
         Ш
         В
         ЯX (якорь X)
         ЯY (якорь Y)
         X0000X
         001100
         010010
         001100
         X0000X
         */

        private List<PatternCell> cells;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<PatternCell> GetEnumerator()
        {
            return cells.GetEnumerator();
        }

        public Pattern() {
            cells = new List<PatternCell>();
        }

        public void Clear() {
            cells.Clear();
        }

        public static void ReadFromFile(string filename, Pattern self) {

            self.Clear();

            using (StreamReader sr = File.OpenText(filename))
            {
                string line;

                line = sr.ReadLine();
                int c = int.Parse(line);

                line = sr.ReadLine();
                int r = int.Parse(line);

                line = sr.ReadLine();
                int anchor_x = int.Parse(line);

                line = sr.ReadLine();
                int anchor_y = int.Parse(line);

                for (int i = 0; i < r; i++)
                {

                    line = sr.ReadLine();

                    for (int j = 0; j < c; j++)
                    {

                        PatternState ps = PatternState.Any;

                        char ch = line[j];
                        switch (ch) {
                            case '0':
                                ps = PatternState.Dead;
                                break;
                            case '1':
                                ps = PatternState.Alive;
                                break;
                            case 'X':
                                break;
                            default:
                                break;
                        }

                        self.cells.Add(new PatternCell(i - anchor_x, j - anchor_y, ps));
                    }
                }
            }
        }
    }

    // { [ (Фигура), (пат1, пат2, ...) ], ... } -- FigurePatternMap
    // { [ (Фигура), (Кол-во распознанных) ], ... } -- MatchResults

    public enum Figure_type {
        Hive
    }

    public class FigurePatternMap : IEnumerable<KeyValuePair<Figure_type, List<Pattern>>> {
        private Dictionary<Figure_type, List<Pattern>> dict;

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Figure_type, List<Pattern>>> GetEnumerator() {
           return dict.GetEnumerator();
        }

        public List<Pattern> this[Figure_type ft] {
            get {
                return dict[ft];    
            }
            set {
                dict[ft] = value;
            }
        }

        public FigurePatternMap()
        {
            dict = new Dictionary<Figure_type, List<Pattern>>();
        }

        public FigurePatternMap(FigureTypePatternFiles parm) : this() {
            foreach (var kvp in parm) {
                List<Pattern> to_add = new List<Pattern>();

                foreach (var pattern_filename in kvp.Value) {
                    Pattern pat = new Pattern();

                    Pattern.ReadFromFile(parm.prefix + pattern_filename, pat);

                    to_add.Add(pat);
                }

                dict.Add(kvp.Key, to_add);
            }
        }

        public void Add(Figure_type ft, IEnumerable<Pattern> array)
        {
            List<Pattern> patterns = new List<Pattern>();

            foreach (var pattern in array)
                patterns.Add(pattern);

            dict.Add(ft, patterns);
        }
    }

    // Хранение путей к файлам с паттернами и установка соответствия между ними
    public class FigureTypePatternFiles : IEnumerable<KeyValuePair<Figure_type, List<string>>> {
        public string prefix;
        Dictionary<Figure_type, List<string>> dict;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Figure_type, List<string>>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        public FigureTypePatternFiles() {
            dict = new Dictionary<Figure_type, List<string>>();

            prefix = @"..\..\..\..\patterns\";

            List<string> list = new List<string>();

            list.Add("hive.txt");

            dict.Add(Figure_type.Hive, list);
        }
    }

    public class PatternMatchingResults : IEnumerable<KeyValuePair<Figure_type, int>> {
        private Dictionary<Figure_type, int> dict;

        public PatternMatchingResults() {

            dict = new Dictionary<Figure_type, int>();

            // Для каждой возможной фигуры добавить запись
            foreach (Figure_type ft in Enum.GetValues(typeof(Figure_type))) {
                dict.Add(ft, 0);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Figure_type, int>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        public int this[Figure_type ft]
        {
            get
            {
                return dict[ft];
            }
            set
            {
                dict[ft] = value;
            }
        }
    }

    public class Board
    {
        public Cell[,] Cells { get; }
        public int CellSize { get; }

        public int Columns { get { return Cells.GetLength(1); } }
        public int Rows { get { return Cells.GetLength(0); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[height / cellSize, width / cellSize];
            for (int x = 0; x < Rows; x++)
                for (int y = 0; y < Columns; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(ProgramSettings ps) : this(ps.Width, ps.Height, ps.cellSize, ps.liveDensity)
        {}

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Rows; x++)
            {
                for (int y = 0; y < Columns; y++)
                {
                    int xL = (x > 0) ? x - 1 : Rows - 1;
                    int xR = (x < Rows - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Columns - 1;
                    int yB = (y < Columns - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public static void WriteToFile(string filename, Board board) {
            using (StreamWriter sw = File.CreateText(filename))
            {
                int c = board.Columns;
                int r = board.Rows;

                sw.WriteLine(c.ToString());
                sw.WriteLine(r.ToString());

                for (int i = 0; i < r; i++)
                {
                    for (int j = 0; j < c; j++)
                    {
                        if (board.Cells[i, j].IsAlive)
                        {
                            sw.Write('1');
                        }
                        else
                        {
                            sw.Write('0');
                        }
                    }
                    sw.Write('\n');
                }
            }
        }

        public static Board ReadFromFile(string filename)
        {
            using (StreamReader sr = File.OpenText(filename))
            {
                string line;

                line = sr.ReadLine();
                int c = int.Parse(line);

                line = sr.ReadLine();
                int r = int.Parse(line);

                Board board = new Board(c, r, 1, 0);

                for (int i = 0; i < r; i++)
                {

                    line = sr.ReadLine();

                    for (int j = 0; j < c; j++)
                    {
                        char ch = line[j];
                        if (ch == '1')
                        {
                            board.Cells[i, j].IsAlive = true;
                        }
                        else
                        {
                            board.Cells[i, j].IsAlive = false;
                        }
                    }
                }

                return board;
            }
        }

        public int CountCells() {
            int result = 0;

            foreach (var cell in Cells) {
                if (cell.IsAlive) result++;
            }

            return result;
        }

        public void Match(FigurePatternMap fpm, out PatternMatchingResults out_pmr) {
            List<PatternCell> banned_cells = new List<PatternCell>();
            out_pmr = new PatternMatchingResults();

            for (int i = 0; i < this.Rows; i++) {
                for (int j = 0; j < this.Columns; j++) {

                    Cell cell = this.Cells[i, j];

                    if (cell.IsAlive)
                    {
                        foreach (var kv_pair in fpm)
                        {
                            foreach (var pattern in kv_pair.Value)
                            {
                                // Цикл для преобразования координат паттерна
                                // Если задан первый бит, то транспонировать
                                // Если второй, то домножить x на -1
                                // Если третий, то y на -1
                                for (int mode = 0; mode <= 0b111; mode++)
                                {
                                    bool pattern_match = true;

                                    foreach (var pattern_cell in pattern)
                                    {
                                        int to_add_x = pattern_cell.x;
                                        int to_add_y = pattern_cell.y;

                                        if ((mode & (1 << 2)) > 0)
                                        {
                                            int temp = to_add_y;
                                            to_add_y = to_add_x;
                                            to_add_x = temp;
                                        }

                                        if ((mode & (1 << 1)) > 0)
                                            to_add_x *= -1;

                                        if ((mode & (1 << 0)) > 0)
                                            to_add_y *= -1;

                                        int new_index_x = (i + to_add_x) % this.Rows;
                                        int new_index_y = (j + to_add_y) % this.Columns;

                                        // Из-за паттерна могут быть отрицательные, деление по модулю это не учитывает
                                        while (new_index_x < 0) new_index_x += this.Rows;
                                        while (new_index_y < 0) new_index_y += this.Columns;

                                        cell = this.Cells[new_index_x, new_index_y];

                                        bool cell_match = true;

                                        switch (pattern_cell.ps)
                                        {
                                            case PatternState.Any:
                                                break;
                                            case PatternState.Alive:
                                                PatternCell cell_to_check = new PatternCell(new_index_x, new_index_y);
                                                bool cell_is_banned = banned_cells.Contains(cell_to_check);

                                                if (!cell.IsAlive || cell_is_banned)
                                                {
                                                    cell_match = false;
                                                }
                                                break;
                                            case PatternState.Dead:
                                                if (cell.IsAlive) cell_match = false;
                                                break;
                                            default:
                                                break;
                                        }

                                        if (!cell_match)
                                        {
                                            pattern_match = false;
                                            break;
                                        }
                                    }

                                    if (pattern_match)
                                    {
                                        out_pmr[kv_pair.Key]++;
                                        banned_cells.Add(new PatternCell(i, j));
                                        goto finish_fugure;
                                    }
                                }
                            }
                        }
                    }

                finish_fugure:
                    ;
                }
            }
        }
    }
    class Program
    {
        static Board board;

        static ProgramSettings ps;
        static private void Reset()
        {
            string filename = "settings.json";

            if (File.Exists(filename))
            {
                ps = ProgramSettings.ReadFromFile(filename);
            }
            else {
                ps = new ProgramSettings(
                    Width: 50,
                    Height: 20,
                    cellSize: 1,
                    liveDensity: 0.5,
                    stepsNeeded: 100
                );

                ProgramSettings.WriteToFile(filename, ps);
            }

            board = new Board(ps);
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[row, col];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static void Main(string[] args)
        {
            Reset();

            string filename = "";

            board = Board.ReadFromFile(filename);

            FigurePatternMap map = new FigurePatternMap(new FigureTypePatternFiles());

            while (true)
            {
                Console.Clear();
                Render();

                PatternMatchingResults match_results;

                board.Match(map, out match_results);

                foreach (var res in match_results) {
                    Console.WriteLine("{0}: {1}", res.Key.ToString(), res.Value);
                }

                board.Advance();
                Thread.Sleep(1000);
            }
        }
    }
}