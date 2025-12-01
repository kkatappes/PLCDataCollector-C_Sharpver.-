using System;

class Program
{
    static void Main()
    {
        string actualResponseHex =
            "D400" +               
            "0000" +               
            "0000" +               
            "00FFFF0300" +         
            "6200" +               
            "0000" +               

            "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" + 
            "0719" +                                                 
            "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +           
            "00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        Console.WriteLine($"Hex文字列長: {actualResponseHex.Length}文字");
        Console.WriteLine($"バイト数: {actualResponseHex.Length / 2}バイト");
        
        // 各パート長を確認
        int headerLen = "D400".Length + "0000".Length + "0000".Length + "00FFFF0300".Length + "6200".Length + "0000".Length;
        Console.WriteLine($"ヘッダ部: {headerLen}文字 ({headerLen/2}バイト)");
        
        int dataLen = actualResponseHex.Length - headerLen;
        Console.WriteLine($"デバイスデータ部: {dataLen}文字 ({dataLen/2}バイト)");
    }
}
