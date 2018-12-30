﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassicUO.IO.Resources
{
    public static class Speeches
    {
        private static SpeechEntry[] _speech;

        public static unsafe void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "speech.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul file = new UOFileMul(path, false);
            List<SpeechEntry> entries = new List<SpeechEntry>();

            while (file.Position < file.Length)
            {
                int id = (file.ReadByte() << 8) | file.ReadByte();
                int length = (file.ReadByte() << 8) | file.ReadByte();

                if (length > 0)
                {
                    entries.Add(new SpeechEntry(id, string.Intern(Encoding.UTF8.GetString((byte*) file.PositionAddress, length))));
                    file.Skip(length);
                }
            }

            _speech = entries.ToArray();
            file.Dispose();
        }

        public static bool IsMatch(string input, SpeechEntry entry)
        {
            int start = 0;

            string[] split = entry.Keywords;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length > 0 && split[i].Length <= input.Length)
                {

                    if (!entry.CheckStart)
                    {
                        if (input.IndexOf(split[i], 0) < 0)
                            continue;
                    }

                    if (!entry.CheckEnd)
                    {
                        if (input.IndexOf(split[i], input.Length - split[i].Length) < 0)
                            continue;
                    }

                    if (input.IndexOf(split[i]) >= 0)
                        return true;
                }
            }
            return false;
        }

        public static SpeechEntry[] GetKeywords(string text)
        {
            if (FileManager.ClientVersion < ClientVersions.CV_305D)
                return new SpeechEntry[0]
                {
                };

            text = text.ToLower();
            List<SpeechEntry> list = new List<SpeechEntry>();

            for (int i = 0; i < _speech.Length; i++)
            {
                SpeechEntry entry = _speech[i];

                if (IsMatch(text, entry))
                    list.Add(entry);
            }

            list.Sort();

            return list.ToArray();
        }
    }
}

public struct SpeechEntry : IComparable<SpeechEntry>
{
    public SpeechEntry(int id, string keyword)
    {
        KeywordID = (short) id;

        Keywords = keyword.Split(new[]
        {
            '*'
        }, StringSplitOptions.RemoveEmptyEntries);

        CheckStart = keyword.Length > 0 && keyword[0] == '*';
        CheckEnd = keyword.Length > 0 && keyword[keyword.Length - 1] == '*';
    }

    public string[] Keywords { get; }

    public short KeywordID { get; }

    public bool CheckStart { get; }

    public bool CheckEnd { get; }

    public int CompareTo(SpeechEntry obj)
    {
        if (KeywordID < obj.KeywordID)
            return -1;

        return KeywordID > obj.KeywordID ? 1 : 0;
    }
}