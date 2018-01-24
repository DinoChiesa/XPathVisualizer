// Tuple.cs
// ------------------------------------------------------------------
//
// simple utility container class.  In lieu of the .NET 4.0 version.
// 
// ------------------------------------------------------------------
//
// Part of XpathVisualizer
//
// Copyright (c) 2009 by Dino Chiesa
// All rights reserved!
//
// ------------------------------------------------------------------

namespace XPathVisualizer
{
    public static class Tuple
    {
        //Allows Tuple.New(1, "2") instead of new Tuple<int, string>(1, "2")
        public static Tuple<T1, T2> New<T1, T2>(T1 v1, T2 v2)
        {
            return new Tuple<T1, T2>(v1, v2);
        }
    }
    
    public class Tuple<T1, T2>
    {
        public Tuple(T1 v1, T2 v2)
        {
            V1 = v1;
            V2 = v2;
        }
    
        public T1 V1 { get; set; }
        public T2 V2 { get; set; }
    }
}
