﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;

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

    public class Board
    {
        public Cell[,] Cells { get; }
        public int CellSize { get; }

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
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
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

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
    }
    class Program
    {
        static Board? board;
        static private void Reset()
        {
            ProgramSettings ps;

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
                    var cell = board.Cells[col, row];
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
            while(true)
            {
                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(1000);
            }
        }
    }
}