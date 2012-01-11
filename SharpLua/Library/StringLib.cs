﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SharpLua.LuaTypes;

namespace SharpLua.Library
{
    public static class StringLib
    {
        public static void RegisterModule(LuaTable enviroment)
        {
            LuaTable module = new LuaTable();
            RegisterFunctions(module);
            enviroment.SetNameValue("string", module);
        }

        public static void RegisterFunctions(LuaTable module)
        {
            module.Register("byte", Byte);
            module.Register("char", Char);
            module.Register("format", Format);
            module.Register("len", Len);
            module.Register("sub", Sub);
            module.Register("lower", Lower);
            module.Register("upper", Upper);
            module.Register("rep", Rep);
            module.Register("reverse", Reverse);
            module.Register("find", Find);
            module.Register("gmatch", GMatch);
            module.Register("match", Match);
            module.Register("gsub", GSub);
        }

        public static LuaValue Byte(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            LuaNumber startNumber = values.Length > 1 ? values[1] as LuaNumber : null;
            LuaNumber endNumber = values.Length > 2 ? values[2] as LuaNumber : null;

            int start = startNumber == null ? 1 : (int)startNumber.Number;
            int end = endNumber == null ? start : (int)endNumber.Number;

            LuaValue[] numbers = new LuaValue[end - start + 1];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = new LuaNumber (char.ConvertToUtf32(str.Text, start - 1 + i) );
            }

            return new LuaMultiValue(numbers);
        }

        public static LuaValue Char(LuaValue[] values)
        {
            char[] chars = new char[values.Length];

            for (int i = 0; i < chars.Length; i++)
            {
                int number = (int)(values[i] as LuaNumber).Number;
                chars[i] = (char)number;
            }

            return new LuaString(new string(chars));
        }

        public static LuaValue Format(LuaValue[] values)
        {
            LuaString format = values[0] as LuaString;
            object[] args = new object[values.Length - 1];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = values[i + 1].Value;
            }
            return new LuaString(string.Format(format.Text, args));
        }

        public static LuaValue Sub(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            LuaNumber startNumber = values[1] as LuaNumber;
            LuaNumber endNumber = values.Length > 2 ? values[2] as LuaNumber : null;

            int start = (int)startNumber.Number;
            int end = endNumber == null ? -1 : (int)endNumber.Number;

            if (start < 0)
            {
                start = str.Text.Length + start + 1;
            }
            if (end < 0)
            {
                end = str.Text.Length + end + 1;
            }

            return new LuaString(str.Text.Substring(start - 1, end - start + 1));
        }

        public static LuaValue Rep(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            LuaNumber number = values[1] as LuaNumber;
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < number.Number; i++)
            {
                text.Append(str.Text);
            }
            return new LuaString(text.ToString());
        }

        public static LuaValue Reverse(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            char[] chars = str.Text.ToCharArray();
            Array.Reverse(chars);
            return new LuaString(new string(chars));
        }

        public static LuaValue Len(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            return new LuaNumber(str.Text.Length);
        }

        public static LuaValue Lower(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            return new LuaString(str.Text.ToLower());
        }

        public static LuaValue Upper(LuaValue[] values)
        {
            LuaString str = values[0] as LuaString;
            return new LuaString(str.Text.ToUpper());
        }
        
        public static LuaValue Find(LuaValue[] args)
        {
            string s = (args[0] as LuaString).Text;
            string format = (args[1] as LuaString).Text;
            int init = 0;
            if (args.Length > 2)
                init = (int.Parse((args[2] as LuaNumber).Number.ToString()) - 1) > 0 ? (int.Parse((args[2] as LuaNumber).Number.ToString()) - 1) : 0;
            bool plain = false;
            if (args.Length > 3)
                plain = (args[3] as LuaBoolean).BoolValue;
            // TODO: return captures
            if (plain)
            {
                int start = s.IndexOf(format);
                int end = format.Length;
                return new LuaMultiValue(new LuaValue[] { new LuaNumber(start), new LuaNumber(end), LuaNil.Nil});
            }
            else
            {
                // TODO: return index also
                return GetMatches(new LuaValue[] { new LuaString(s.Substring(init)), new LuaString(format) }).GetValue(0);
            }
        }
        
        /// <summary>
        /// The string.gmatch function, credit to Arjen Douma
        /// </summary>
        /// <param name="args">the args</param>
        /// <returns>An iterator for use in for loops (for-in)</returns>
        public static LuaValue GMatch(LuaValue[] args)
        {
            LuaFunction f = new LuaFunction(BaseLib.Next);
            LuaTable t = GetMatches(args);
            return new LuaMultiValue(new LuaValue[] { f, t, LuaNil.Nil});
        }
        
        private static LuaTable GetMatches(LuaValue[] args)
        {
            LuaTable t = new LuaTable();
            
            string s = (args[0] as LuaString).Text;
            string format = (args[1] as LuaString).Text;
            
            Regex re = new Regex(format);
            Match m = re.Match(s);
            
            while (m.Success)
            {
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    Group g = m.Groups[i];
                    CaptureCollection cc = g.Captures;
                    for (int j = 0; j < cc.Count; j++)
                    {
                        Capture c = cc[j];
                        t.AddValue(new LuaString(c.Value));
                    }
                }
                m = m.NextMatch();
            }
            return t;
        }
        
        public static LuaValue Match(LuaValue[] args)
        {
            string s = args[0].ToString();
            string format = args[1].ToString();
            int init = 0;
            if (args.Length > 2)
                init = (int) (args[3] as LuaNumber).Number;
            return GetMatches(new LuaValue[] { new LuaString(s.Substring(init)), new LuaString(format) });
        }
        
        public static LuaValue GSub(LuaValue[] args)
        {
            int totalReplaces = 0;
            // string.gsub (s, pattern, repl [, n])
            string s = args[0].ToString();
            string format = args[1].ToString();
            LuaValue r = args[2];
            int n = -1;
            if (args.Length > 3)
                n = int.Parse(args[4].ToString());
            
            Regex re = new Regex(format);
            Match m = re.Match(s);
            
            // a lot of wierd random things occur here...
            while (m.Success)
            {
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    Group g = m.Groups[i];
                    CaptureCollection cc = g.Captures;
                    for (int j = 0; j < cc.Count; j++)
                    {
                        Capture c = cc[j];
                        if (r is LuaString)
                            s = s.Substring(0, c.Index) + (r as LuaString).Text + s.Substring(c.Length);
                        if (r is LuaFunction)
                            s = s.Substring(0, c.Index) + (r as LuaFunction).Invoke(new LuaValue[] { new LuaString(c.Value) }).ToString() + s.Substring(c.Length);
                        if (r is LuaTable)
                            s = s.Substring(0, c.Index) + ((r as LuaTable).GetValue(c.Value) == null ? s.Substring(c.Index, c.Length) : (r as LuaTable).GetValue(c.Value).ToString()) + s.Substring(c.Length);
                        totalReplaces++;
                    }
                }
                m = m.NextMatch();
            }
            // TODO: return Multi Value
            LuaTable t = new LuaTable();
            t.SetNameValue("replaces", new LuaNumber(totalReplaces));
            t.SetNameValue("string", new LuaString(s));
            return t;
        }
    }
}
