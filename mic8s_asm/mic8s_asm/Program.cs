using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mic8s_asm
{
    class Program
    {
        private static readonly string version = "v1.1.0";
        private static readonly List<string> op1 = new List<string>()
        {
            "nop", "option", "sleep", "clrwdt", "return", "retfie", "daa", "das",
            "int", "movwp", "clrw", "movwp++", "movwp--", "imovw++", "imovw--",
            "imovf++", "imovf--", "imovw", "imovf"
        };

        private static readonly List<string> op2 = new List<string>()
        {
            "tris", "brsz", "brcz", "brsc", "brcc", "movlc", "movli", "movlb",
            "loop", "movwf", "clrf", "goto", "call", "sublw", "addlw", "andlw",
            "xorlw", "iorlw", "retlw", "movlw", "movlt", "imovw", "imovf",
            "subwf", "decf", "iorwf", "andwf", "xorwf", "addwf", "movf",
            "comf", "incf", "decfsz", "rrf", "rlf", "swapf", "incfsz", 
            "bcf", "bsf", "btfsc", "btfss", "adcwf", "sbcwf"
        };

        private static readonly List<string> op3 = new List<string>()
        {
            "subwf", "decf", "iorwf", "andwf", "xorwf", "addwf", "movf",
            "comf", "incf", "decfsz", "rrf", "rlf", "swapf", "incfsz", 
            "bcf", "bsf", "btfsc", "btfss", "adcwf", "sbcwf"
        };

        private static readonly List<string> opx = new List<string>()
        {
            "brsz", "brcz", "brsc", "brcc", "loop", "movlt", "movlc", "movli"
        };

        private static readonly List<string> opq = new List<string>()
        {
            "imovw", "imovf", "imovw+", "imovw-", "imovf+", "imovf-"
        };

        private static readonly Dictionary<string, UInt16> Insts = new Dictionary<string, ushort>()
        {
            {"nop", 0x0000}, {"option", 0x0002}, {"sleep", 0x0003}, {"clrwdt", 0x0004}, 
            {"tris", 0x0005}, {"return", 0x0008}, {"retfie", 0x0009}, {"daa", 0x000a}, 
            {"das", 0x000b}, {"int", 0x000f}, {"movwp", 0x000c}, {"movwf", 0x0080}, 
            {"clrw", 0x0001}, {"clrf", 0x0180}, {"subwf", 0x0200}, {"decf", 0x0300}, 
            {"iorwf", 0x0400}, {"andwf", 0x0500}, {"xorwf", 0x0600}, {"addwf", 0x0700}, 
            {"movf", 0x0800}, {"comf", 0x0900}, {"incf", 0x0a00}, {"decfsz", 0x0b00}, 
            {"rrf", 0x0c00}, {"rlf", 0x0d00}, {"swapf", 0x0e00}, {"incfsz", 0x0f00}, 
            {"bcf", 0x1000}, {"bsf", 0x1400}, {"btfsc", 0x1800}, {"btfss", 0x1c00}, 
            {"goto", 0x2000}, {"call", 0x3000}, {"sublw", 0x2800}, {"addlw", 0x2900},
            {"andlw", 0x2c00}, {"xorlw", 0x2d00}, {"iorlw", 0x3800}, {"adcwf", 0x3a00}, 
            {"sbcwf", 0x3b00}, {"retlw", 0x3c00}, {"movlw", 0x3d00}, {"brsz", 0x0010},
            {"brcz", 0x0011}, {"brsc", 0x0012}, {"brcc", 0x0013}, {"loop", 0x0015},
            {"movlt", 0x0016}, {"movlc", 0x0014}, {"imovw", 0x0018}, {"imovw++", 0x0019},
            {"imovw--", 0x001a}, {"imovf", 0x001c}, {"imovf++", 0x001d}, {"imovf--", 0x001e},
            {"imovw+", 0x0100}, {"imovf+", 0x0120}, {"movwp++", 0x000d}, {"movwp--", 0x000e},
            {"imovw-", 0x0110}, {"imovf-", 0x0130}, {"movli", 0x0017}, {"movlb", 0x0020}
        };

        public List<UInt16> bincode = new List<UInt16>();
        public SortedDictionary<UInt16, List<string>> cseg = new SortedDictionary<UInt16, List<string>>();
        public Dictionary<string, UInt16> labels = new Dictionary<string, UInt16>();
        public Dictionary<string, byte> local_defs = new Dictionary<string, byte>();
        public Dictionary<string, byte> global_defs = new Dictionary<string, byte>();
        private UInt16 cstart = 0;
        private UInt16 address = 0;
        public string filename = string.Empty;

        static void Main(string[] args)
        {
            var app = new Program();
            bool is_abin = false;
            bool is_pp = false;
            bool arg_err = true;
            
            /*
            args = new string[3];
            args[0] = "-l";
            args[1] = "-f";
            args[2] = "test.asm";
             */

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-a"))
                {
                    is_abin = true;
                }
                else if (args[i].Equals("-l"))
                {
                    is_pp = true;
                }
                else if (args[i].Equals("-f") && !string.IsNullOrEmpty(args[i + 1]))
                {
                    arg_err = false;
                    app.filename = args[++i];
                }
                else
                {
                    arg_err = true;
                    break;
                }
            }
            
            if(arg_err)
            {
                app.showUsage();
                return;
            }

            if (!string.IsNullOrEmpty(app.filename) && !File.Exists(app.filename))
            {
                Console.WriteLine(">> {0} file not found!", app.filename);
                return;
            }

            if (!app.pp_asm())
            {
                Console.WriteLine(">> assembly failed while do pre-analysis!");
                return;
            }

            if (!app.pp_asm2())
            {
                Console.WriteLine(">> assembly failed while do pre-analysis!");
                return;
            }

            if (is_pp)
            {
                app.do_ppfile();
            }

            app.do_asm();

            if (is_abin)
            {
                app.do_abin();
            }
        }

        private bool pp_asm()
        {
            bool is_okay = true;
            cstart = 0;
            address = 0;

            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                int lineno = 0;

                // code start from address 0x000 by default
                cstart = 0;
                cseg.Add(0, new List<string>());

                while ((line = sr.ReadLine()) != null)
                {
                    pp_line(line, lineno++);
                }
            }

            return is_okay;
        }

        private bool pp_asm2()
        {
            foreach (var item in cseg)
            {
                var codes = item.Value;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!do_pp_asm2(ref codes, codes[i], item.Key))
                        return false;
                }
            }

            return true;
        }

        private void do_asm()
        {
            ushort baddress = 0;
            ushort address = 0;
            int length = 0;
            byte sublen = 0;
            int count = 0;
            byte parity = 0;
            byte btmp = 0;
            string line = string.Empty;

            string fpath = Path.GetPathRoot(this.filename);
            string fname = Path.GetFileNameWithoutExtension(this.filename);
            fname += ".hex";
            fpath = Path.Combine(fpath, fname);

            using (StreamWriter sw = new StreamWriter(fpath))
            {
                foreach (var item in cseg)
                {
                    var codes = item.Value;
                    for (int i = 0; i < codes.Count; i++)
                    {
                        do_inst(ref codes, codes[i], item.Key);
                    }

                    address = item.Key;
                    length = codes.Count;
                    count = 0;
                    baddress = (ushort)(address * 2);

                    while(length > 0)
                    {
                        sublen = length > 8 ? (byte)8 : (byte)length;
                        parity = (byte)((int)sublen*2);
                        line = string.Format(":{0:x2}", parity);
                        btmp = (byte)((baddress >> 8) & 0xff);
                        parity += btmp;
                        line += string.Format("{0:x2}", btmp);
                        btmp = (byte)(baddress & 0xff);
                        parity += btmp;
                        line += string.Format("{0:x2}", btmp);
                        line += "00";

                        for (int i = 0; i < sublen; i++)
                        {
                            ushort code = System.Convert.ToUInt16(codes[i+count]);
                            code &= 0x3fff;
                            btmp = (byte)(code & 0xff);
                            parity += btmp;
                            line += string.Format("{0:x2}", btmp);
                            btmp = (byte)((code >> 8) & 0xff);
                            parity += btmp;
                            line += string.Format("{0:x2}", btmp);
                        }

                        line += string.Format("{0:x2}", (byte)(~parity + 1));

                        sw.WriteLine(line);

                        length -= sublen;
                        count += sublen;
                        address += sublen;
                        baddress += (ushort)(sublen * 2);
                    }
                }

                // end of file
                sw.WriteLine(":00000001ff");
            }
        }

        private bool do_pp_asm2(ref List<string> codes, string inst, UInt16 cstart)
        {
            string dstr;
            byte bval = 0;
            UInt16[] uval = new UInt16[3];

            int index = codes.IndexOf(inst);
            string[] iseg = inst.Split(" ".ToCharArray());

            if (iseg[0].Equals(".db"))
            {
                dstr = iseg[1];
                if (!pp_toByte(dstr, ref bval))
                {
                    if (dstr.Equals("$"))
                    {
                        bval = (byte)(cstart + index);
                    }
                    else if (local_defs.ContainsKey(dstr))
                    {
                        bval = local_defs[dstr];
                    }
                    else if (global_defs.ContainsKey(dstr))
                    {
                        bval = global_defs[dstr];
                    }
                    else
                    {
                        Console.WriteLine("Error: symbol \"{0}\" not found", dstr);
                        return false;
                    }
                }

                codes[index] = string.Format(".db {0:x2}h", bval);
            }
            if (iseg[0].Equals(".dc"))
            {
                dstr = iseg[1];
                if (!pp_toUint16(dstr, ref uval[0]))
                {
                    if (dstr.Equals("$"))
                    {
                        uval[0] = (ushort)(cstart + index);
                    }
                    else if (labels.ContainsKey(dstr))
                    {
                        uval[0] = labels[dstr];
                    }
                    else if (local_defs.ContainsKey(dstr))
                    {
                        uval[0] = local_defs[dstr];
                    }
                    else if (global_defs.ContainsKey(dstr))
                    {
                        uval[0] = global_defs[dstr];
                    }
                    else
                    {
                        Console.WriteLine("Error: symbol \"{0}\" not found", dstr);
                        return false;
                    }
                }

                codes[index] = string.Format(".dc {0:x3}h", uval[0]);
            }
            else if (iseg.Length > 1)
            {
                string qlabel = string.Empty;
                codes[index] = iseg[0];

                if (opq.Contains(iseg[0]))
                {
                    qlabel = iseg[0].Substring(iseg[0].Length-1, 1);
                    //iseg[1] = iseg[1].TrimStart("+-".ToCharArray());
                }

                for (int i = 1; i < iseg.Length; i++)
                {
                    if (!pp_toUint16(iseg[i], ref uval[i]))
                    {
                        if (iseg[i].Equals("$"))
                        {
                            uval[i] = (UInt16)(cstart + index);
                        }
                        else if (local_defs.ContainsKey(iseg[i]))
                        {
                            uval[i] = (UInt16)local_defs[iseg[i]];
                        }
                        else if (global_defs.ContainsKey(iseg[i]))
                        {
                            uval[i] = (UInt16)global_defs[iseg[i]];
                        }
                        else if (labels.ContainsKey(iseg[i]))
                        {
                            uval[i] = labels[iseg[i]];
                        }
                        else
                        {
                            Console.WriteLine("Error: symbol \"{0}\" not found", iseg[i]);
                            return false;
                        }
                    }
                }

                if (iseg.Length == 2)
                {
                    codes[index] = string.Format("{0} {2:x2}h", iseg[0], qlabel, uval[1]);
                }
                else
                {
                    codes[index] = string.Format("{0} {1:x2}h {2:x1}", iseg[0], uval[1], uval[2]);
                }
            }

            return true;
        }

        private void do_inst(ref List<string> codes, string inst, UInt16 cstart)
        {
            byte bval = 0;
            UInt16[] uval = new UInt16[3];

            int index = codes.IndexOf(inst);
            string[] iseg = inst.Split(" ".ToCharArray());

            if (iseg[0].Equals(".db"))
            {
                pp_toByte(iseg[1], ref bval);
                codes[index] = bval.ToString();
            }
            else if (iseg[0].Equals(".dc"))
            {
                pp_toUint16(iseg[1], ref uval[0]);
                codes[index] = uval[0].ToString();
            }
            else if ((iseg.Length == 1) || opx.Contains(iseg[0]))
            {
                codes[index] = Insts[iseg[0]].ToString();
            }
            else if (opq.Contains(iseg[0]) && (iseg.Length == 2))
            {
                UInt16 icode = Insts[iseg[0]];

                pp_toUint16(iseg[1], ref uval[0]);
                codes[index] = (icode | (uval[0] & 0xf)).ToString();
            }
            else
            {
                UInt16 icode = Insts[iseg[0]];

                for (int i = 1; i < iseg.Length; i++)
                {
                    //uval[i] = System.Convert.ToUInt16(iseg[i]);
                    pp_toUint16(iseg[i], ref uval[i]);
                }

                if (iseg.Length == 2)
                {
                    if (icode == 0x0005 || icode == 0x0020) // tris, movlb
                    {
                        codes[index] = ((icode & 0x3ff0) | (uval[1] & 7)).ToString();
                    }
                    else if ((icode == 0x2000) || (icode == 0x3000)) // goto, call
                    {
                        codes[index] = (icode | (uval[1] & 0x7ff)).ToString();
                    }
                    else if ((icode == 0x3a00) || (icode == 0x3b00)) // adcwf, sbcwf
                    {
                        codes[index] = (icode | (uval[1] & 0x7f)).ToString();
                    }
                    else if ((icode & 0x2000) == 0x2000)
                    {
                        // sublw, addlw, andlw, xorlw, iorlw, retlw, movlw
                        codes[index] = (icode | (uval[1] & 0xff)).ToString();
                    }
                    else
                    {
                        // all others
                        codes[index] = (icode | (uval[1] & 0x7f)).ToString();
                    }
                }
                else if (iseg.Length == 3)
                {
                    if ((icode & 0x1000) == 0x1000) // bcf, bsf, btfsc, btfss
                    {
                        codes[index] = (icode | (uval[1] & 0x007f) | ((uval[2] & 0x7) << 7)).ToString();
                    }
                    else
                    {
                        codes[index] = (icode | (uval[1] & 0x007f) | ((uval[2] & 0x1) << 7)).ToString();
                    }
                }
            }
        }

        private void do_ppfile()
        {
            string code;
            ushort address = 0;
            string fpath = Path.GetPathRoot(this.filename);
            string fname = Path.GetFileNameWithoutExtension(this.filename);
            fname += ".lst";
            fpath = Path.Combine(fpath, fname);

            using (StreamWriter sw = new StreamWriter(fpath))
            {
                foreach (var codes in cseg)
                {
                    code = string.Format("\t.org 0x{0:x3}", codes.Key);
                    sw.WriteLine(code);
                    address = codes.Key;

                    foreach (var inst in codes.Value)
                    {
                        string opcode = inst.Split(" ".ToCharArray())[0];

                        if (!opcode.Equals(".dc"))
                        {
                            code = string.Format("[{0:x3}]:\t{1}", address++, inst);
                            sw.WriteLine(code);
                        }
                        else
                        {
                            address++;
                        }
                    }

                    sw.WriteLine();
                }
            }
        }

        private void do_abin()
        {
            int cstart = 0;
            string ccode = string.Empty;
            string fpath = Path.GetPathRoot(this.filename);
            string fname = Path.GetFileNameWithoutExtension(this.filename);
            fname += ".abin";
            fpath = Path.Combine(fpath, fname);
            List<string> clist = new List<string>();

            var litem = cseg.Last();
            int maxlen = litem.Key + litem.Value.Count;

            // NOTE: change maxlen to 1040
            for (int i = 0; i < 1040; i++)
                clist.Add("11111111111111");

            using (StreamWriter sw = new StreamWriter(fpath))
            {
                foreach (var codes in cseg)
                {
                    cstart = codes.Key;
                    
                    for (int i = 0; i < codes.Value.Count; i++)
                    {
                        ccode = pp_toAbin(codes.Value[i]);
                        clist[cstart + i] = ccode;
                    }
                }

                foreach (var line in clist)
                {
                    sw.WriteLine(line);
                }
            }
        }

        private void pp_line(string line, int lineno)
        {
            byte bval = 0;
            //UInt16 uval = 0;

            if (string.IsNullOrEmpty(line))
                return;

            line = line.Trim();

            if (line.StartsWith(";") || line.StartsWith("//"))
                return;

            string[] segs = line.Split(" ,\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            List<string> list = new List<string>();
            foreach (var item in segs)
            {
                if (item.StartsWith(";"))
                    break;
                
                list.Add(item.ToLower());
            }

            if (list.Count == 0)
                return;

            if (list[0].ToLower().Equals(".org"))
            {
                if (list.Count != 2 || !pp_isNumber(list[1]) || !pp_toUint16(list[1], ref cstart))
                {
                    Console.WriteLine("Error:{0}: {1}", lineno, line);
                    return;
                }

                address = cstart;
                // add a new code segment
                if (!cseg.ContainsKey(cstart))
                {
                    cseg.Add(cstart, new List<string>());
                }
                
            }
            else if (list.Count > 1 && list[1].Equals(".equ"))
            {
                if (list.Count != 3 || !pp_isNumber(list[2]) || !pp_toByte(list[2], ref bval))
                {
                    Console.WriteLine("Error:{0}: {1}", lineno, line);
                    return;
                }

                if (local_defs.ContainsKey(list[0]))
                {
                    Console.WriteLine("Error:{0}: symbol {1} redefined", lineno, list[0]);
                    return;
                }
                else
                {
                    local_defs.Add(list[0], bval);
                }
            }
            else if (list[0].Equals(".local"))
            {
                if (list.Count != 3 || !pp_isNumber(list[2]) || !pp_toByte(list[2], ref bval))
                {
                    Console.WriteLine("Error:{0}: {1}", lineno, line);
                    return;
                }

                if (local_defs.ContainsKey(list[1]))
                {
                    Console.WriteLine("Error:{0}: symbol {1} redefined", lineno, list[1]);
                    return;
                }
                else
                {
                    local_defs.Add(list[1], bval);
                }
            }
            else if (list[0].Equals(".global"))
            {
                if (list.Count != 3 || !pp_isNumber(list[2]) || !pp_toByte(list[2], ref bval))
                {
                    Console.WriteLine("Error:{0}: {1}", lineno, line);
                    return;
                }

                if (global_defs.ContainsKey(list[1]))
                {
                    Console.WriteLine("Error:{0}: symbol {1} redefined", lineno, list[1]);
                    return;
                }
                else
                {
                    global_defs.Add(list[1], bval);
                }
            }
            else if (list[0].Equals(".db"))
            {
                if (list.Count != 2 || !pp_toByte(list[1], ref bval))
                {
                    Console.WriteLine("Error:({0}): {1}", lineno, line);
                    return;
                }

                address++;
                cseg[cstart].Add(string.Format(".db {0}", bval));
            }
            else if (list[0].EndsWith(":"))
            {
                //UInt16 offset = 0;
                labels.Add(list[0].TrimEnd(":".ToCharArray()), address);

                if (list.Count > 1)
                {
                    Console.WriteLine("Error:{0}: {1}", lineno, line);
                    Console.WriteLine("\t  Don't mix label and code in same line!");
                    return;
                }
            }
            else if (Insts.ContainsKey(list[0]) && list.Count < 4)
            {
                if(!op1.Contains(list[0]) && list.Count == 1)
                {
                    Console.WriteLine("Error:{0}: {1} with wrong operand", lineno, list[0]);
                    return;
                }

                if (!op2.Contains(list[0]) && list.Count == 2)
                {
                    Console.WriteLine("Error:{0}: {1} with wrong operand", lineno, list[0]);
                    return;
                }

                if (!op3.Contains(list[0]) && list.Count == 3)
                {
                    Console.WriteLine("Error:{0}: {1} with wrong operand", lineno, list[0]);
                    return;
                }

                if (opq.Contains(list[0]))
                {
                    if (list.Count > 2)
                    {
                        Console.WriteLine("Error:{0}: {1} with wrong operand", lineno, list[0]);
                        return;
                    }

                    if (list.Count == 2)
                    {
                        bool is_p = list[1].StartsWith("+");
                        bool is_m = list[1].StartsWith("-");
                        bool is_q = is_p || is_m;
                        if (!is_q)
                        {
                            Console.WriteLine("Error:{0}: {1} with wrong operand", lineno, list[0]);
                            return;
                        }
                        else
                        {
                            list[0] += is_p ? "+" : "-";
                            list[1] = list[1].TrimStart("+-".ToCharArray());
                        }
                    }
                }

                string ccc = string.Empty;

                foreach (var item in list)
                {
                    ccc += item + " ";
                }

                address++;
                cseg[cstart].Add(ccc.Trim());

                if (opx.Contains(list[0]))
                {
                    cseg[cstart].Add(string.Format(".dc {0}", list[1]));
                    address++;
                }
            }
            else
            {
                Console.WriteLine("Error:{0}: {1} instruction error", lineno, line);
                return;
            }
        }

        private void showUsage()
        {
            Console.WriteLine("LogicGreen MIC8S assembly ({0}) usage:", version);
            Console.WriteLine("> mic8s_asm.exe [-OPTION] -f filename");
            Console.WriteLine("\tOPTION:");
            Console.WriteLine("\t -l : generate list file");
            Console.WriteLine("\t -a : generate abin file");
            Console.WriteLine("\t -f filename : given assembly input file");
        }

        public bool pp_toByte(string str, ref byte value)
        {
            str = str.Trim();
            byte btmp = 0;

            try
            {
                if (str.EndsWith("h") || str.StartsWith("0x"))
                {
                    if (str.StartsWith("0x"))
                        str = str.Substring(2);

                    btmp = System.Convert.ToByte(str.TrimEnd("h".ToCharArray()), 16);
                }
                else if (str.EndsWith("o") || str.EndsWith("q"))
                {
                    btmp = System.Convert.ToByte(str.TrimEnd("oq".ToCharArray()), 8);
                }
                else
                {
                    btmp = System.Convert.ToByte(str, 10);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Warning: {0}", ex.Message);
                return false;
            }

            value = btmp;
            return true;
        }

        public bool pp_toUint16(string str, ref UInt16 value)
        {
            str = str.Trim();
            UInt16 utmp = 0;

            try
            {

                if (str.EndsWith("h") || str.StartsWith("0x"))
                {
                    if (str.StartsWith("0x"))
                        str = str.Substring(2);

                    utmp = System.Convert.ToUInt16(str.TrimEnd("h".ToCharArray()), 16);
                }
                else if (str.EndsWith("o") || str.EndsWith("q"))
                {
                    utmp = System.Convert.ToUInt16(str.TrimEnd("oq".ToCharArray()), 8);
                }
                else
                {
                    utmp = System.Convert.ToUInt16(str, 10);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Warning: {0}", ex.Message);
                return false;
            }

            value = utmp;
            return true;
        }

        public bool pp_isNumber(string str)
        {
            str = str.Trim().ToLower();

            if (str.StartsWith("0x") || Char.IsDigit(str[0]))
                return true;

            return false;
        }

        public string pp_toAbin(string inst)
        {
            string abin = string.Empty;

            ushort ucode = Convert.ToUInt16(inst);
            string atmp = Convert.ToString(ucode, 2);

            int alen = atmp.Length > 14 ? 14 : atmp.Length;

            for (int i = 0; i < 14 - alen; i++)
            {
                abin += "0";
            }

            abin += atmp;

            return abin;
        }
    }
}
