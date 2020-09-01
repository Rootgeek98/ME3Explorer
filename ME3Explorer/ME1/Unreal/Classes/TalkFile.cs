﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;
using ME3ExplorerCore.Gammtek.IO;
using ME3ExplorerCore.Packages;

namespace ME1Explorer.Unreal.Classes
{
    public class TalkFile : ITalkFile, IEquatable<TalkFile>
    {
        #region structs
        public struct HuffmanNode
        {
            public int LeftNodeID;
            public int RightNodeID;
            public char data;

            public HuffmanNode(int r, int l)
                : this()
            {
                RightNodeID = r;
                LeftNodeID = l;
            }

            public HuffmanNode(char c)
                : this()
            {
                data = c;
                LeftNodeID = -1;
                RightNodeID = -1;
            }
        }

        [DebuggerDisplay("TLKStringRef {StringID} {Data}")]
        public class TLKStringRef : ME3Explorer.NotifyPropertyChangedBase, IEquatable<TLKStringRef>, IComparable
        {
            private int _stringID;
            private string _data;
            private int _flags;
            private int _index;


            public int StringID { get => _stringID; set => SetProperty(ref _stringID, value); }
            public string Data { get => _data; set => SetProperty(ref _data, value); }
            public int Flags { get => _flags; set => SetProperty(ref _flags, value); }
            public int Index { get => _index; set => SetProperty(ref _index, value); }
            public int BitOffset { get => _flags; set => SetProperty(ref _flags, value); } //use same variable to save memory as flags is not used in me2/3, but bitoffset is.

            /// <summary>
            /// This is used by huffman compression
            /// </summary>
            public string ASCIIData
            {
                get
                {
                    if (Data == null)
                    {
                        return "-1\0";
                    }
                    if (Data.EndsWith("\0"))
                    {
                        return Data;
                    }
                    return Data + '\0';
                }
            }

            public TLKStringRef(BinaryReader r, bool me1)
            {
                StringID = r.ReadInt32();
                if (me1)
                {
                    Flags = r.ReadInt32();
                    Index = r.ReadInt32();
                }
                else
                {
                    BitOffset = r.ReadInt32();
                }
            }

            public TLKStringRef(int id, int flags, string data, int index = -1)
            {
                StringID = id;
                Flags = flags;
                Data = data;
                Index = index;
            }

            public bool Equals(TLKStringRef other)
            {
                return StringID == other.StringID && ASCIIData == other.ASCIIData && Flags == other.Flags /*&& Index == other.Index*/;
            }
            public int CompareTo(object obj)
            {
                TLKStringRef entry = (TLKStringRef)obj;
                return Index.CompareTo(entry.Index);
            }
        }
        #endregion

        private List<HuffmanNode> nodes;
        private BitArray Bits;
        private int langRef;
        private readonly int tlkSetUIndex;

        public TLKStringRef[] StringRefs;
        public IMEPackage pcc;
        //public int index;
        public int uindex;

        public int LangRef
        {
            get => langRef;
            set { langRef = value; language = pcc.GetNameEntry(value); }
        }

        public string language;
        public bool male;

        public string Name => pcc.GetUExport(uindex).ObjectName;
        public string BioTlkSetName => tlkSetUIndex != 0 ? pcc.getObjectName(tlkSetUIndex) : null;


        #region Constructors
        public TalkFile(IMEPackage _pcc, int uindex)
        {
            pcc = _pcc;
            //index = _index;
            this.uindex = uindex;
            tlkSetUIndex = 0;
            LoadTlkData();
        }

        public TalkFile(ExportEntry export)
        {
            if (export.FileRef.Game != MEGame.ME1)
            {
                throw new Exception("ME1 Unreal TalkFile cannot be initialized with a non-ME1 file");
            }
            pcc = export.FileRef;
            uindex = export.UIndex;
            tlkSetUIndex = 0;
            LoadTlkData();
        }
        
        public TalkFile(IMEPackage _pcc, int uindex, bool _male, int _langRef, int _tlkSetUIndex)
        {
            pcc = _pcc;
            //index = _index;
            this.uindex = uindex;
            LangRef = _langRef;
            male = _male;
            tlkSetUIndex = _tlkSetUIndex;
            LoadTlkData();
        }
        #endregion

        //ITalkFile
        public string findDataById(int strRefID, bool withFileName = false)
        {
            string data = "No Data";
            foreach (TLKStringRef tlkStringRef in StringRefs)
            {
                if (tlkStringRef.StringID == strRefID)
                {
                    data = $"\"{tlkStringRef.Data}\"";
                    if (withFileName)
                    {
                        data += $" ({Path.GetFileName(pcc.FilePath)} -> {BioTlkSetName}.{Name})";
                    }
                    break;
                }
            }
            return data;
        }

        #region IEquatable
        public bool Equals(TalkFile other)
        {
            return (other?.uindex == uindex && other.pcc.FilePath == pcc.FilePath);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as TalkFile);
        }

        public override int GetHashCode()
        {
            return 1;
        }
        #endregion

        #region Load Data
        public void LoadTlkData(EndianReader r = null)
        {
            r ??= new EndianReader(new MemoryStream(pcc.GetUExport(uindex).GetBinaryData()), Encoding.Unicode)
            {
                Endian = pcc.Endian
            };
            //hashtable
            int entryCount = r.ReadInt32();
            StringRefs = new TLKStringRef[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                StringRefs[i] = new TLKStringRef(r, true);
            }

            //Huffman tree
            nodes = new List<HuffmanNode>();
            int nodeCount = r.ReadInt32();
            for (int i = 0; i < nodeCount; i++)
            {
                bool leaf = r.ReadBoolean();
                if (leaf)
                {
                    nodes.Add(new HuffmanNode(r.ReadChar()));
                }
                else
                {
                    nodes.Add(new HuffmanNode(r.ReadInt16(), r.ReadInt16()));
                }
            }
            //TraverseHuffmanTree(nodes[0], new List<bool>());

            //encoded data
            int stringCount = r.ReadInt32();
            byte[] data = new byte[r.BaseStream.Length - r.BaseStream.Position];
            r.Read(data, 0, data.Length);
            Bits = new BitArray(data);

            //decompress encoded data with huffman tree
            int offset = 4;
            var rawStrings = new List<string>(stringCount);
            while (offset * 8 < Bits.Length)
            {
                int size = BitConverter.ToInt32(data, offset);
                offset += 4;
                string s = GetString(offset * 8);
                offset += size + 4;
                rawStrings.Add(s);
            }

            //associate StringIDs with strings
            foreach (TLKStringRef strRef in StringRefs)
            {
                if (strRef.Flags == 1)
                {
                    strRef.Data = rawStrings[strRef.Index];
                }
            }
        }

        private string GetString(int bitOffset)
        {
            HuffmanNode root = nodes[0];
            HuffmanNode curNode = root;

            string curString = "";
            int i;
            for (i = bitOffset; i < Bits.Length; i++)
            {
                /* reading bits' sequence and decoding it to Strings while traversing Huffman Tree */
                int nextNodeID = Bits[i] ? curNode.RightNodeID : curNode.LeftNodeID;

                /* it's an internal node - keep looking for a leaf */
                if (nextNodeID >= 0)
                    curNode = nodes[nextNodeID];
                else
                /* it's a leaf! */
                {
                    char c = curNode.data;
                    if (c != '\0')
                    {
                        /* it's not NULL */
                        curString += c;
                        curNode = root;
                        i--;
                    }
                    else
                    {
                        /* it's a NULL terminating processed string, we're done */
                        //skip ahead approximately 9 bytes to the next string
                        return curString;
                    }
                }
            }

            if (curNode.LeftNodeID == curNode.RightNodeID)
            {
                char c = curNode.data;
                //We hit edge case where final bit is on a byte boundary and there is nothing left to read. This is a leaf node.
                if (c != '\0')
                {
                    /* it's not NULL */
                    curString += c;
                    curNode = root;
                }
                else
                {
                    /* it's a NULL terminating processed string, we're done */
                    //skip ahead approximately 9 bytes to the next string
                    return curString;
                }
            }

            Debug.WriteLine("RETURNING NULL STRING (NOT NULL TERMINATED)!");
            return null;
        }

        private void TraverseHuffmanTree(HuffmanNode node, List<bool> code)
        {
            /* check if both sons are null */
            if (node.LeftNodeID == node.RightNodeID)
            {
                BitArray ba = new BitArray(code.ToArray());
                string c = "";
                foreach (bool b in ba)
                {
                    c += b ? '1' : '0';
                }
            }
            else
            {
                /* adds 0 to the code - process left son*/
                code.Add(false);
                TraverseHuffmanTree(nodes[node.LeftNodeID], code);
                code.RemoveAt(code.Count - 1);

                /* adds 1 to the code - process right son*/
                code.Add(true);
                TraverseHuffmanTree(nodes[node.RightNodeID], code);
                code.RemoveAt(code.Count - 1);
            }
        }
        #endregion

        public void saveToFile(string fileName)
        {
            XmlTextWriter xr = new XmlTextWriter(fileName, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 4
            };

            xr.WriteStartDocument();
            xr.WriteStartElement("tlkFile");
            xr.WriteAttributeString("Name", Name);

            foreach (TLKStringRef tlkStringRef in StringRefs)
            {
                xr.WriteStartElement("string");

                xr.WriteStartElement("id");
                xr.WriteValue(tlkStringRef.StringID);
                xr.WriteEndElement(); // </id>
                xr.WriteStartElement("flags");
                xr.WriteValue(tlkStringRef.Flags);
                xr.WriteEndElement(); // </flags>

                //if (i == StringRefs.Length - 1)
                //{
                //    Debugger.Break();
                //}
                if (tlkStringRef.Flags != 1)
                    xr.WriteElementString("data", "-1");
                else
                    xr.WriteElementString("data", tlkStringRef.Data);

                xr.WriteEndElement(); // </string>
            }

            xr.WriteEndElement(); // </tlkFile>
            xr.Flush();
            xr.Close();
        }
        public string TLKtoXmlstring()
        {
            StringBuilder InputTLK = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(InputTLK))
            {
                using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;

                    writer.WriteStartDocument();
                    writer.WriteStartElement("tlkFile");
                    writer.WriteAttributeString("Name", Name);

                    foreach (TLKStringRef tlkStringRef in StringRefs)
                    {
                        writer.WriteStartElement("string");
                        writer.WriteStartElement("id");
                        writer.WriteValue(tlkStringRef.StringID);
                        writer.WriteEndElement(); // </id>
                        writer.WriteStartElement("flags");
                        writer.WriteValue(tlkStringRef.Flags);
                        writer.WriteEndElement(); // </flags>
                        if (tlkStringRef.Flags != 1)
                            writer.WriteElementString("data", "-1");
                        else
                            writer.WriteElementString("data", tlkStringRef.Data);
                        writer.WriteEndElement(); // </string>
                    }
                    writer.WriteEndElement(); // </tlkFile>
                }
            }
            return InputTLK.ToString();
        }


    }
}