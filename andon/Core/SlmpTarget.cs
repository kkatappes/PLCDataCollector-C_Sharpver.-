using System;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP通信対象設定クラス
    /// Python: util.py での target 相当
    /// </summary>
    public class SlmpTarget
    {
        private byte _network = 0;
        private byte _node = 0;
        private ushort _destinationProcessor = 0;
        private byte _multiDropStation = 0;

        /// <summary>
        /// ネットワーク番号 (0-255)
        /// </summary>
        public byte Network
        {
            get => _network;
            set
            {
                _network = value;
            }
        }

        /// <summary>
        /// ノード番号 (0-255)
        /// </summary>
        public byte Node
        {
            get => _node;
            set
            {
                _node = value;
            }
        }

        /// <summary>
        /// 宛先プロセッサ番号 (0-65535)
        /// </summary>
        public ushort DestinationProcessor
        {
            get => _destinationProcessor;
            set
            {
                _destinationProcessor = value;
            }
        }

        /// <summary>
        /// マルチドロップ局番号 (0-255)
        /// </summary>
        public byte MultiDropStation
        {
            get => _multiDropStation;
            set
            {
                _multiDropStation = value;
            }
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// 全パラメータをデフォルト値 (0) で初期化
        /// </summary>
        public SlmpTarget()
        {
        }

        /// <summary>
        /// パラメータ指定コンストラクタ
        /// </summary>
        /// <param name="network">ネットワーク番号</param>
        /// <param name="node">ノード番号</param>
        /// <param name="destinationProcessor">宛先プロセッサ番号</param>
        /// <param name="multiDropStation">マルチドロップ局番号</param>
        public SlmpTarget(byte network, byte node, ushort destinationProcessor = 0, byte multiDropStation = 0)
        {
            Network = network;
            Node = node;
            DestinationProcessor = destinationProcessor;
            MultiDropStation = multiDropStation;
        }

        /// <summary>
        /// ターゲット設定が空（すべて0）かどうかを判定
        /// </summary>
        /// <returns>すべて0の場合はtrue</returns>
        public bool IsEmpty()
        {
            return Network == 0 && Node == 0 && DestinationProcessor == 0 && MultiDropStation == 0;
        }

        /// <summary>
        /// ターゲット設定をリセット（すべて0に設定）
        /// </summary>
        public void Reset()
        {
            Network = 0;
            Node = 0;
            DestinationProcessor = 0;
            MultiDropStation = 0;
        }

        /// <summary>
        /// 他のSlmpTargetとの値比較
        /// </summary>
        /// <param name="other">比較対象</param>
        /// <returns>値が等しい場合はtrue</returns>
        public bool Equals(SlmpTarget? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Network == other.Network &&
                   Node == other.Node &&
                   DestinationProcessor == other.DestinationProcessor &&
                   MultiDropStation == other.MultiDropStation;
        }

        /// <summary>
        /// Equals オーバーライド
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>値が等しい場合はtrue</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as SlmpTarget);
        }

        /// <summary>
        /// GetHashCode オーバーライド
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Network, Node, DestinationProcessor, MultiDropStation);
        }

        /// <summary>
        /// SlmpTarget の文字列表現を取得
        /// </summary>
        /// <returns>SlmpTarget(Network,Node,DestinationProcessor,MultiDropStation) 形式の文字列</returns>
        public override string ToString()
        {
            return $"SlmpTarget({Network},{Node},{DestinationProcessor},{MultiDropStation})";
        }

        /// <summary>
        /// 等価演算子
        /// </summary>
        public static bool operator ==(SlmpTarget? left, SlmpTarget? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// 非等価演算子
        /// </summary>
        public static bool operator !=(SlmpTarget? left, SlmpTarget? right)
        {
            return !(left == right);
        }
    }
}