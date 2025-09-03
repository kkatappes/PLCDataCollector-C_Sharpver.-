# 開発指針 (t-wadaの手法)

このプロジェクトでは、品質の高いコードを維持するためにt-wadaの手法を採用します。

## テストファースト開発

**基本サイクル**: Red-Green-Refactor
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

```python
# 例: 新機能開発時
# 1. テストを先に書く
def test_feature_importance_ranking():
    features = ['density', 'temperature', 'humidity']
    importance = [0.8, 0.3, 0.1] 
    result = rank_features_by_importance(features, importance)
    assert result[0] == 'density'

# 2. 最小実装
def rank_features_by_importance(features, importance):
    return sorted(features, key=lambda x: importance[features.index(x)], reverse=True)

# 3. リファクタリング
def rank_features_by_importance(features, importance):
    feature_importance = list(zip(features, importance))
    return [f for f, _ in sorted(feature_importance, key=lambda x: x[1], reverse=True)]
```

## リファクタリング原則

- **外部動作を変えない**: 機能仕様は維持したまま内部構造を改善
- **小さなステップ**: 一度に一つの変更のみ実行
- **テストで保護**: リファクタリング前後でテストが通ることを確認

## 境界値テスト

入力値の境界付近でのバグを防ぐテスト設計:

```python
# データ分析における境界値テストの例
def test_feature_selection_boundaries():
    # 境界値: 最小/最大特徴量数
    assert select_features([], threshold=0.5) == []  # 空配列
    assert len(select_features(single_feature, threshold=0.5)) <= 1  # 単一特徴量
    
    # 閾値境界
    assert select_features(features, threshold=0.0)  # 最小閾値
    assert select_features(features, threshold=1.0)  # 最大閾値
```

## テスト可能な設計

### 1. 依存性注入
```python
# テストしやすい設計例
class ModelTrainer:
    def __init__(self, data_loader, preprocessor, model):
        self.data_loader = data_loader
        self.preprocessor = preprocessor  
        self.model = model
    
    def train(self, config):
        data = self.data_loader.load(config.data_path)
        processed_data = self.preprocessor.transform(data)
        return self.model.fit(processed_data)

# テスト時
def test_model_training():
    mock_loader = Mock()
    mock_preprocessor = Mock()
    mock_model = Mock()
    
    trainer = ModelTrainer(mock_loader, mock_preprocessor, mock_model)
    trainer.train(config)
    
    mock_model.fit.assert_called_once()
```

### 2. 純粋関数の活用
```python
# 副作用のない関数（テストしやすい）
def calculate_feature_importance(coefficients, feature_names):
    """係数から特徴量重要度を計算（純粋関数）"""
    return dict(zip(feature_names, np.abs(coefficients)))

# 外部状態に依存しない設計
def preprocess_data(raw_data, encoding='cp932'):
    """データ前処理（外部ファイル読み込みを分離）"""
    return raw_data.fillna(0).apply(lambda x: x.str.encode(encoding))
```

### 3. 単一責任原則
各モジュールは明確に分離された責任を持つ:
- `data_io/`: データの入出力のみ
- `preprocessing/`: データ変換のみ  
- `modeling/`: モデル訓練のみ
- `evaluation/`: 評価指標計算のみ

## 実装ガイドライン

- **疎結合**: モジュール間の依存を最小化
- **高凝集**: 関連機能を同一モジュールに集約
- **インターフェース分離**: 必要な機能のみ公開
- **依存性逆転**: 具象ではなく抽象に依存

これらの原則により、保守性・拡張性・テスト性を兼ね備えた堅牢なMLパイプラインを構築します。