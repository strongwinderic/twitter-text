// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using System;

namespace TwitterText
{
    public class Range : IComparable<Range>
    {

        public static Range EMPTY = new Range(-1, -1);

        public int start;
        public int end;

        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public override bool Equals(Object obj)
        {
            return this == obj || obj is Range && ((Range)obj).start == this.start &&
                ((Range)obj).end == this.end;
        }

        public override int GetHashCode()
        {
            return 31 * start + 31 * end;
        }

        public int CompareTo(Range other)
        {
            if (this.start < other.start)
            {
                return -1;
            }
            else if (this.start == other.start)
            {
                if (this.end < other.end)
                {
                    return -1;
                }
                else
                {
                    return this.end == other.end ? 0 : 1;
                }
            }
            else
            {
                return 1;
            }
        }

        public bool IsInRange(int val)
        {
            return val >= start && val <= end;
        }
    }
}
