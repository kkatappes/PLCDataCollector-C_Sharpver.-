using System;

namespace SlmpClient.Constants
{
    /// <summary>
    /// SLMP コマンドコード定義
    /// Python: const.SLMPCommand (118項目)
    /// </summary>
    public enum SlmpCommand : ushort
    {
        #region Device Operations (デバイス操作)
        
        /// <summary>デバイス読み取り</summary>
        Device_Read = 0x0401,
        
        /// <summary>デバイス書き込み</summary>
        Device_Write = 0x1401,
        
        /// <summary>ランダムデバイス読み取り</summary>
        Device_ReadRandom = 0x0403,
        
        /// <summary>ランダムデバイス書き込み</summary>
        Device_WriteRandom = 0x1402,
        
        /// <summary>モニタデバイス登録</summary>
        Device_EntryMonitorDevice = 0x0801,
        
        /// <summary>モニタ実行</summary>
        Device_ExecuteMonitor = 0x0802,
        
        /// <summary>ブロック読み取り</summary>
        Device_ReadBlock = 0x0406,
        
        /// <summary>ブロック書き込み</summary>
        Device_WriteBlock = 0x1406,
        
        #endregion
        
        #region Label Operations (ラベル操作)
        
        /// <summary>配列ラベル読み取り</summary>
        Label_ArrayLabelRead = 0x041A,
        
        /// <summary>配列ラベル書き込み</summary>
        Label_ArrayLabelWrite = 0x141A,
        
        /// <summary>ランダムラベル読み取り</summary>
        Label_LabelReadRandom = 0x041C,
        
        /// <summary>ランダムラベル書き込み</summary>
        Label_LabelWriteRandom = 0x141B,
        
        #endregion
        
        #region Memory Operations (メモリ操作)
        
        /// <summary>メモリ読み取り</summary>
        Memory_Read = 0x0613,
        
        /// <summary>メモリ書き込み</summary>
        Memory_Write = 0x1613,
        
        #endregion
        
        #region Extend Unit Operations (拡張ユニット操作)
        
        /// <summary>拡張ユニット読み取り</summary>
        ExtendUnit_Read = 0x0601,
        
        /// <summary>拡張ユニット書き込み</summary>
        ExtendUnit_Write = 0x1601,
        
        #endregion
        
        #region Remote Control Operations (リモート制御操作)
        
        /// <summary>リモートRUN</summary>
        RemoteControl_RemoteRun = 0x1001,
        
        /// <summary>リモートSTOP</summary>
        RemoteControl_RemoteStop = 0x1002,
        
        /// <summary>リモートPAUSE</summary>
        RemoteControl_RemotePause = 0x1003,
        
        /// <summary>リモートラッチクリア</summary>
        RemoteControl_RemoteLatchClear = 0x1005,
        
        /// <summary>リモートリセット</summary>
        RemoteControl_RemoteReset = 0x1006,
        
        /// <summary>型名読み取り</summary>
        RemoteControl_ReadTypeName = 0x0101,
        
        /// <summary>ノード表示</summary>
        RemoteControl_NodeIndication = 0x3070,
        
        #endregion
        
        #region Drive Operations (ドライブ操作)
        
        /// <summary>ディスク状態読み取り</summary>
        Drive_ReadDiskState = 0x0205,
        
        /// <summary>デフラグ</summary>
        Drive_Defrag = 0x1207,
        
        #endregion
        
        #region Remote Password Operations (リモートパスワード操作)
        
        /// <summary>リモートパスワードロック</summary>
        RemotePassword_Lock = 0x1631,
        
        /// <summary>リモートパスワードアンロック</summary>
        RemotePassword_Unlock = 0x1630,
        
        #endregion
        
        #region File Operations (ファイル操作)
        
        /// <summary>ファイル情報読み取り</summary>
        File_ReadFileInfo = 0x0201,
        
        /// <summary>タイトル付きファイル情報読み取り</summary>
        File_ReadFileInfoWithTitle = 0x0202,
        
        /// <summary>ファイル番号情報読み取り</summary>
        File_ReadFileNoInfo = 0x0204,
        
        /// <summary>ファイル情報変更</summary>
        File_ChangeFileInfo = 0x1204,
        
        /// <summary>ファイル検索</summary>
        File_Search = 0x0203,
        
        /// <summary>ファイル読み取り</summary>
        File_Read = 0x0206,
        
        /// <summary>ファイル書き込み</summary>
        File_Write = 0x1203,
        
        /// <summary>ファイルロック</summary>
        File_FileLock = 0x0808,
        
        /// <summary>ファイルコピー</summary>
        File_Copy = 0x1206,
        
        /// <summary>ファイル削除</summary>
        File_Delete = 0x1205,
        
        /// <summary>ディレクトリ読み取り</summary>
        File_ReadDir = 0x1810,
        
        /// <summary>ディレクトリ検索</summary>
        File_SearchDir = 0x1811,
        
        /// <summary>新規ファイル作成A</summary>
        File_NewFileA = 0x1202,
        
        /// <summary>新規ファイル作成B</summary>
        File_NewFileB = 0x1820,
        
        /// <summary>ファイル削除</summary>
        File_DeleteFile = 0x1822,
        
        /// <summary>ファイルコピー</summary>
        File_CopyFile = 0x1824,
        
        /// <summary>ファイル状態変更</summary>
        File_ChangeFileState = 0x1825,
        
        /// <summary>ファイル日付変更</summary>
        File_ChangeFileDate = 0x1826,
        
        /// <summary>ファイルオープン</summary>
        File_OpenFile = 0x1827,
        
        /// <summary>ファイル読み取り</summary>
        File_ReadFile = 0x1828,
        
        /// <summary>ファイル書き込み</summary>
        File_WriteFile = 0x1829,
        
        /// <summary>ファイルクローズ</summary>
        File_CloseFile = 0x182A,
        
        #endregion
        
        #region System Commands (システムコマンド)
        
        /// <summary>セルフテスト</summary>
        SelfTest = 0x0619,
        
        /// <summary>エラーコードクリア</summary>
        ClearError_Code = 0x1617,
        
        /// <summary>エラー履歴クリア</summary>
        ClearError_History = 0x1619,
        
        /// <summary>オンデマンド</summary>
        OnDemand = 0x2101,
        
        #endregion
        
        #region Data Collection (データ収集)
        
        /// <summary>データ収集認証</summary>
        DataCollection_Auth = 0x4000,
        
        /// <summary>データ収集キープアライブ</summary>
        DataCollection_KeepAlive = 0x4001,
        
        /// <summary>データ収集データ取得</summary>
        DataCollection_GetData = 0x4002,
        
        /// <summary>データ収集配信</summary>
        DataCollection_Distribute = 0x4003,
        
        #endregion
        
        #region Node Connection (ノード接続)
        
        /// <summary>ノード検索</summary>
        NodeConnection_NodeSearch = 0x0E30,
        
        /// <summary>IPアドレス設定</summary>
        NodeConnection_IPAddressSet = 0x0E31,
        
        #endregion
        
        #region Parameter Setting (パラメータ設定)
        
        /// <summary>デバイス情報比較</summary>
        ParameterSetting_DeviceInfoCompare = 0x0E32,
        
        /// <summary>パラメータ取得</summary>
        ParameterSetting_ParameterGet = 0x0E33,
        
        /// <summary>パラメータ更新</summary>
        ParameterSetting_ParameterUpdate = 0x0E34,
        
        /// <summary>パラメータ設定開始</summary>
        ParameterSetting_ParameterSetStart = 0x0E35,
        
        /// <summary>パラメータ設定終了</summary>
        ParameterSetting_ParameterSetEnd = 0x0E36,
        
        /// <summary>パラメータ設定キャンセル</summary>
        ParameterSetting_ParameterSetCancel = 0x0E3A,
        
        /// <summary>デバイス識別情報取得</summary>
        ParameterSetting_DeviceIdentificationInfoGet = 0x0E28,
        
        /// <summary>通信速度設定</summary>
        ParameterSetting_CommunicationSpeed = 0x3072,
        
        #endregion
        
        #region Node Monitoring (ノードモニタリング)
        
        /// <summary>状態読み取り</summary>
        NodeMonitoring_StatusRead = 0x0E44,
        
        /// <summary>状態読み取り2</summary>
        NodeMonitoring_StatusRead2 = 0x0E53,
        
        /// <summary>接続設定取得</summary>
        NodeMonitoring_ConnectionSettingGet = 0x0E45,
        
        /// <summary>データモニタリング</summary>
        NodeMonitoring_DataMonitoring = 0x0E29,
        
        #endregion
        
        #region Other Protocols (その他プロトコル)
        
        /// <summary>CAN通信</summary>
        Other_CAN = 0x4020,
        
        /// <summary>IO-Link通信</summary>
        Other_IOLInk = 0x5000,
        
        /// <summary>Modbus TCP通信</summary>
        Other_ModbusTCP = 0x5001,
        
        /// <summary>Modbus RTU通信</summary>
        Other_ModbusRTU = 0x5002,
        
        #endregion
        
        #region CC-Link IE Field Diagnostics (CC-Link IEフィールド診断)
        
        /// <summary>選択ノード情報取得</summary>
        CCLinkIEFieldDiagnostics_SelectNodeInfoGet = 0x3119,
        
        /// <summary>通信テスト</summary>
        CCLinkIEFieldDiagnostics_CommunicationTest = 0x3040,
        
        /// <summary>ケーブルテスト</summary>
        CCLinkIEFieldDiagnostics_CableTest = 0x3050,
        
        #endregion
        
        #region CC-Link IE TSN Network Management (CC-Link IE TSNネットワーク管理)
        
        /// <summary>ネットワーク設定</summary>
        CCLinkIETSNNetworkManagement_NetworkConfig = 0x0E90,
        
        /// <summary>マスタ設定</summary>
        CCLinkIETSNNetworkManagement_MasterConfig = 0x0E91,
        
        /// <summary>スレーブ設定</summary>
        CCLinkIETSNNetworkManagement_SlaveConfig = 0x0E92,
        
        /// <summary>サイクリック設定</summary>
        CCLinkIETSNNetworkManagement_CyclicConfig = 0x0E93,
        
        /// <summary>通知</summary>
        CCLinkIETSNNetworkManagement_Notification = 0x0E94,
        
        #endregion
        
        #region Link Device Parameter (リンクデバイスパラメータ)
        
        /// <summary>リンクデバイスパラメータ書き込み</summary>
        LinkDeviceParameter_LinkDevicePrmWrite = 0x320A,
        
        /// <summary>リンクデバイスパラメータ書き込みチェック要求</summary>
        LinkDeviceParameter_LinkDevicePrmWriteCheckReq = 0x320B,
        
        /// <summary>リンクデバイスパラメータ書き込みチェック応答</summary>
        LinkDeviceParameter_LinkDevicePrmWriteCheckResp = 0x320C,
        
        #endregion
        
        #region Event History (イベント履歴)
        
        /// <summary>イベント数取得</summary>
        EventHistory_GetEventNum = 0x3060,
        
        /// <summary>イベント履歴取得</summary>
        EventHistory_GetEventHistory = 0x3061,
        
        /// <summary>イベント履歴クリア</summary>
        EventHistory_ClearEventHistory = 0x161A,
        
        /// <summary>クロックオフセットデータ送信</summary>
        EventHistory_ClockOffsetDataSend = 0x3062,
        
        #endregion
        
        #region Backup/Restore (バックアップ/リストア)
        
        /// <summary>通信設定取得</summary>
        BackupRestore_GetCommunicationSet = 0x0EB0,
        
        /// <summary>局サブID一覧取得</summary>
        BackupRestore_GetStationSubIDList = 0x0EB1,
        
        /// <summary>デバイス情報取得</summary>
        BackupRestore_GetDeviceInfo = 0x0EB2,
        
        /// <summary>バックアップ開始</summary>
        BackupRestore_StartBackup = 0x0EB3,
        
        /// <summary>バックアップ終了</summary>
        BackupRestore_EndBackup = 0x0EB4,
        
        /// <summary>バックアップ要求</summary>
        BackupRestore_RequestBackup = 0x0EB5,
        
        /// <summary>バックアップパラメータ取得</summary>
        BackupRestore_GetBackupPrm = 0x0EB6,
        
        /// <summary>リストアチェック</summary>
        BackupRestore_CheckRestore = 0x0EB7,
        
        /// <summary>リストア開始</summary>
        BackupRestore_StartRestore = 0x0EB8,
        
        /// <summary>リストア終了</summary>
        BackupRestore_EndRestore = 0x0EB9,
        
        /// <summary>バックアップパラメータ設定</summary>
        BackupRestore_SetBackupPrm = 0x0EBA,
        
        /// <summary>スレーブ局パラメータリストアチェック</summary>
        SlaveStationPrmRestore_CheckPrmDelivery = 0x0EBE,
        
        #endregion
        
        #region Start/Stop Cyclic (サイクリック開始/停止)
        
        /// <summary>自局サイクリック停止</summary>
        StartStopCyclic_StopOwnStationCyclic = 0x3206,
        
        /// <summary>自局サイクリック開始</summary>
        StartStopCyclic_StartOwnStationCyclic = 0x3207,
        
        /// <summary>他局サイクリック停止</summary>
        StartStopCyclic_StopOtherStationCyclic = 0x3208,
        
        /// <summary>他局サイクリック開始</summary>
        StartStopCyclic_StartOtherStationCyclic = 0x3209,
        
        #endregion
        
        #region Reserved Station (予約局)
        
        /// <summary>予約局設定一時解除</summary>
        ReservedStation_RsvStationConfigTemporaryRelease = 0x320D,
        
        /// <summary>予約局設定</summary>
        ReservedStation_RsvStationConfig = 0x320E,
        
        #endregion
        
        #region Watchdog Counter (ウォッチドッグカウンタ)
        
        /// <summary>ウォッチドッグカウンタ情報設定</summary>
        WatchdogCounter_SetWatchdogCounterInfo = 0x3210,
        
        /// <summary>ウォッチドッグカウンタオフセット設定</summary>
        WatchdogCounter_WatchdogCounterOffsetConfig = 0x3211,
        
        #endregion
    }
}