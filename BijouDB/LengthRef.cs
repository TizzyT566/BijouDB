﻿namespace BijouDB;

public class LengthRef
{
    private long _length = 0;
    public static implicit operator long(LengthRef lengthRef) => lengthRef._length;
    public static LengthRef operator +(LengthRef src, long amnt)
    {
        src._length += amnt;
        return src;
    }
}