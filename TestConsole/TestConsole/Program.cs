
// WordlPuzzle
//
// Implementation of a program to solve word puzzles like those published in newspapers.
//
// Programmer:  Jorge L. Orejel
// Last update: 12/31/2014
// Based on:    C++ code written on 12/30/2004 and included in my unpublished textbook
//              "Applied Algorithms and Data Structures", and submitted to The Code Project

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WordPuzzle
{
    class Program
    {
        // Global data members

        public static char[,] puzzle = new char[Constants.maxN, Constants.maxM],   // puzzle array
                                solution = new char[Constants.maxN, Constants.maxM]; // solution array

        public static int n,       // actual number of rows in puzzle and solutionn arrays
                          m,       // actual number of columns in puzzle and solutionn arrays
                          max_n_m, // number of elements in main diagonals of puzzle and solutionn arrays
                          wordFP;  // fingerprint of a word (pattern)

        static void Main(string[] args)
        {
            FileStream fs = null;
            StreamReader sr = null;
            string word;

            if (LoadPuzzle())
            {
                ShowPuzzle();

                try
                {
                    fs = new FileStream(Constants.wordsFile, FileMode.Open);
                    sr = new StreamReader(fs);

                    while ((word = sr.ReadLine()) != null)
                    {
                        FindWord(word, puzzle);
                    }
                    sr.Close();
                    fs.Close();

                    ShowSolution();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
            }
        }

        static bool LoadPuzzle()
        {
            bool ok = false;
            FileStream fs = null;
            StreamReader sr = null;
            string line;

            try
            {
                fs = new FileStream(Constants.puzzleFile, FileMode.Open);
                sr = new StreamReader(fs);

                n = m = 0;

                while ((line = sr.ReadLine()) != null)
                {
                    int len = line.Length;

                    if (m == 0)
                    {
                        m = len - (len >> 1);
                    }
                    for (int j = 0, k = 0; j < len; j += 2, ++k)
                    {
                        puzzle[n, k] = line[j];
                    }
                    ++n;
                }
                max_n_m = n < m ? m : n;
                sr.Close();
                fs.Close();
                ok = true;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
            }
            return ok;
        }

        static void ShowPuzzle()
        {
            Console.WriteLine(String.Format("\nPuzzle array ({0} x {1}):\n", n, m));

            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < m; ++j)
                {
                    Console.Write(String.Format("{0} ", puzzle[i, j]));
                    solution[i, j] = ' ';
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void FindWord(string word, char[,] puzzle)
        {
            int wordLenght = word.Length;

            wordFP = WordFingerprint(word, wordLenght);

            Console.Write(String.Format("Searching for '{0,12}', fingerprint == {1,6}",
                                          word, wordFP));

            if (!(HorizontalKRsearch(word, wordLenght)
                    ||
                    VerticalKRsearch(word, wordLenght)
                    ||
                    SouthEastKRsearch(word, wordLenght)
                    ||
                    SouthWestKRsearch(word, wordLenght)))
            {
                Console.Write(" not");
            }
            Console.WriteLine(" found");
        }

        static int WordFingerprint(string word, int wordLength)
        {
            int sum = 0;

            for (int i = 0; i < wordLength; ++i)
            {
                sum += (int)(word[i]);
            }
            return sum;
        }

        static bool HorizontalKRsearch(string word, int wordLength)
        {
            int i;
            FillInfo fillInfo;

            unsafe
            {
                for (i = 0; i < n; ++i)
                {
                    fixed (char* puzzleRow = &puzzle[i, 0], solutionRow = &solution[i, 0])
                    {
                        if (HorizontalOrVerticalKRsearch(word,
                                                           puzzleRow,
                                                           wordLength,
                                                           m,
                                                           1,
                                                           out fillInfo))
                        {
                            HorizontalOrVerticalFillWord(solutionRow, word, wordLength, 1, fillInfo);
                            break;
                        }
                    }
                }
            }
            return i < n;
        }

        static bool VerticalKRsearch(string word, int wordLength)
        {
            int j;
            FillInfo fillInfo;

            unsafe
            {
                for (j = 0; j < m; ++j)
                {
                    fixed (char* puzzleCol = &puzzle[0, j], solutionCol = &solution[0, j])
                    {
                        if (HorizontalOrVerticalKRsearch(word,
                                                           puzzleCol,
                                                           wordLength,
                                                           n,
                                                           Constants.maxM,
                                                           out fillInfo))
                        {
                            HorizontalOrVerticalFillWord(solutionCol, word, wordLength, Constants.maxM, fillInfo);
                            break;
                        }
                    }
                }
            }
            return j < m;
        }

        unsafe static bool HorizontalOrVerticalKRsearch(string pattern,
                                                         char* target,
                                                         int patternLenght,
                                                         int targetLength,
                                                         int displacement,
                                                         out FillInfo fillInfo)
        {
            int i, j, k, targetFP,
                patternMaxIndex = patternLenght * displacement,
                targetMaxIndex = (targetLength - patternLenght) * displacement;

            fillInfo = null;
            targetFP = HorizontalOrVerticalFingerprint(target, 0, patternMaxIndex, displacement);

            for (i = 0; i <= targetMaxIndex; i += displacement)
            {
                if (wordFP == targetFP)
                {
                    // Must check for an exact match either in the forward or in the backward direction.
                    // Even though the word (or pattern) 'alpha' has the same fingerprint as the target
                    // 'aplah', they DO NOT match because of the scrambling of characters in the target.

                    for (j = 0, k = i; j < patternLenght; ++j, k += displacement) // forward match
                    {
                        if (pattern[j] != target[k]) // no match
                            break;
                    }
                    if (j == patternLenght) // match
                    {
                        fillInfo = new FillInfo(i, Direction.forward);
                        return true;
                    }
                    else
                    {
                        for (j = patternLenght - 1, k = i; j >= 0; --j, k += displacement) // backward match
                        {
                            if (pattern[j] != target[k]) // no match
                                break;
                        }
                        if (j == -1) // match
                        {
                            fillInfo = new FillInfo(i, Direction.backward);
                            return true;
                        }
                    }
                }
                else // "slide" the pattern over the target by one position and adjust the target's fingerprint
                {
                    targetFP += target[i + patternMaxIndex] - target[i];
                }
            }
            return false;
        }

        unsafe static int HorizontalOrVerticalFingerprint(char* puzzleStr, int start, int length, int displacement)
        {
            int sum = 0;

            for (int j = start; j < length; j += displacement)
            {
                sum += puzzleStr[j];
            }
            return sum;
        }

        unsafe static void HorizontalOrVerticalFillWord(char* solutionStr, string word, int wordLen,
                                                         int displacement, FillInfo fillInfo)
        {
            int i, j = fillInfo.position;

            if (fillInfo.direction == Direction.forward)
            {
                for (i = 0; i < wordLen; ++i)
                {
                    solutionStr[j] = word[i];
                    j += displacement;
                }
            }
            else // fillInfo.direction == Direction.backward
            {
                for (i = wordLen - 1; i >= 0; --i)
                {
                    solutionStr[j] = word[i];
                    j += displacement;
                }
            }
        }

        static bool SouthEastKRsearch(string word, int wordLength)
        {
            int i, targetLength = max_n_m;
            bool found = false;
            FillInfo fillInfo;

            unsafe
            {
                for (i = 0; wordLength <= targetLength; ++i)
                {
                    fixed (char* puzzleRowDiagStart = &puzzle[i, 0],
                                  solutionRowDiagStart = &solution[i, 0])
                    {
                        if (DiagonalKRsearch(word, puzzleRowDiagStart,
                                               wordLength, targetLength, Constants.maxM, out fillInfo))
                        {
                            found = true;
                            DiagonalFillWord(solutionRowDiagStart, word, wordLength, Constants.maxM, fillInfo);
                            break;
                        }
                        --targetLength;
                    }
                }
                if (targetLength < wordLength)
                {
                    targetLength = max_n_m - 1;
                    for (i = 1; wordLength <= targetLength; ++i)
                    {
                        fixed (char* puzzleColDiagStart = &puzzle[0, i], solutionColDiagStart = &solution[0, i])
                        {
                            if (DiagonalKRsearch(word, puzzleColDiagStart,
                                                   wordLength, targetLength, Constants.maxM, out fillInfo))
                            {
                                found = true;
                                DiagonalFillWord(solutionColDiagStart, word, wordLength, Constants.maxM, fillInfo);
                                break;
                            }
                            --targetLength;
                        }
                    }
                }
            }
            return found;
        }

        static bool SouthWestKRsearch(string word, int wordLength)
        {
            int i, targetLength = max_n_m;
            bool found = false;
            FillInfo fillInfo;

            unsafe
            {
                for (i = m - 1; wordLength <= targetLength; --i)
                {
                    fixed (char* puzzleRowDiagStart = &puzzle[0, i], solutionRowDiagStart = &solution[0, i])
                    {
                        if (DiagonalKRsearch(word,
                                               puzzleRowDiagStart,
                                               wordLength,
                                               targetLength,
                                               Constants.maxM - 2,
                                               out fillInfo))
                        {
                            found = true;
                            DiagonalFillWord(solutionRowDiagStart, word, wordLength, Constants.maxM - 2, fillInfo);
                            break;
                        }
                        --targetLength;
                    }
                }
                if (targetLength < wordLength)
                {
                    targetLength = max_n_m - 1;
                    for (i = 1; wordLength <= targetLength; ++i)
                    {
                        fixed (char* puzzleColDiagStart = &puzzle[i, m - 1], solutionColDiagStart = &solution[i, m - 1])
                        {
                            if (DiagonalKRsearch(word,
                                                   puzzleColDiagStart,
                                                   wordLength,
                                                   targetLength,
                                                   Constants.maxM - 2,
                                                   out fillInfo))
                            {
                                found = true;
                                DiagonalFillWord(solutionColDiagStart, word, wordLength, Constants.maxM - 2, fillInfo);
                                break;
                            }
                            --targetLength;
                        }
                    }
                }
            }
            return found;
        }

        unsafe static bool DiagonalKRsearch(string pattern, char* target,
                                             int patternLenght, int targetLength, int displacement,
                                             out FillInfo fillInfo)
        {
            int i, j, k, targetFP,
                patternMaxIndex = patternLenght * (displacement + 1),
                targetMaxIndex = (targetLength - patternLenght) * (displacement + 1);

            fillInfo = null;
            targetFP = DiagonalFingerprint(target, 0, patternMaxIndex, displacement);

            for (i = 0; i <= targetMaxIndex; i += displacement + 1)
            {
                if (wordFP == targetFP)
                {
                    // Must check for an exact match either in the forward or in the backward direction.
                    // Even though the word (or pattern) 'alpha' has the same fingerprint as the target
                    // 'aplah', they DO NOT match because of the scrambling of characters in the target.

                    for (j = 0, k = i; j < patternLenght; ++j, k += displacement + 1) // forward match
                    {
                        if (pattern[j] != target[k]) // no match
                            break;
                    }
                    if (j == patternLenght) // match
                    {
                        fillInfo = new FillInfo(i, Direction.forward);
                        return true;
                    }
                    else
                    {
                        for (j = patternLenght - 1, k = i; j >= 0; --j, k += displacement + 1) // backward match
                        {
                            if (pattern[j] != target[k]) // no match
                                break;
                        }
                        if (j == -1) // match
                        {
                            fillInfo = new FillInfo(i, Direction.backward);
                            return true;
                        }
                    }
                }
                else // "slide" the pattern over the target by one position and adjust the target's fingerprint
                {
                    targetFP += target[i + patternMaxIndex] - target[i];
                }
            }
            return false;
        }

        unsafe static int DiagonalFingerprint(char* puzzleStr, int start, int length, int displacement)
        {
            int sum = 0;

            for (int j = start; j < length; j += displacement + 1)
            {
                sum += puzzleStr[j];
            }
            return sum;
        }

        unsafe static void DiagonalFillWord(char* solutionStr, string word, int wordLen,
                                          int displacement, FillInfo fillInfo)
        {
            int i, j = fillInfo.position;

            if (fillInfo.direction == Direction.forward)
            {
                for (i = 0; i < wordLen; ++i)
                {
                    solutionStr[j] = word[i];
                    j += displacement + 1;
                }
            }
            else // fillInfo.direction == Direction.backward
            {
                for (i = wordLen - 1; i >= 0; --i)
                {
                    solutionStr[j] = word[i];
                    j += displacement + 1;
                }
            }
        }

        static void ShowSolution()
        {
            Console.WriteLine(String.Format("\nSolution ({0} x {1}):\n", n, m));

            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < m; ++j)
                {
                    Console.Write(String.Format("{0} ", solution[i, j]));
                    solution[i, j] = ' ';
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }

    public static class Constants
    {
        public static readonly int maxN = 50, maxM = 50;

        public static readonly string puzzleFile = "wpuzzle.txt",
                                      wordsFile = "words.txt";
    }

    public enum Direction { forward, backward }; // Direction to fill a word in the solution

    public class FillInfo
    {
        public int position;        // Position to start filling a word
        public Direction direction; // Fill direction

        public FillInfo(int _position, Direction _direction)
        {
            position = _position;
            direction = _direction;
        }
    }
}
