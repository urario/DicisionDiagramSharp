# Unit Test Review

Follow-up execution plan: `docs/v0.7-test-quality-remediation-plan.md`.

Backlog tracking: `docs/backlog.md` section `v0.7.1-v0.7.5 Test Quality Remediation Plan`.

## 1. Executive Summary

- 全体評価: Core の主要演算はかなり広くテストされており、特に ZDD は naive model との比較があり、BDD も explicit truth table を一部持っている点は良いです。一方で、BDD の巨大クラスに仕様テスト、数学性質テスト、canonicalization、例外、境界値、white-box coverage テストが混在しており、保守性とレビュー容易性を大きく落としています。
- 最も重大な問題: private reflection による内部型・内部メソッド・内部フィールド依存が通常の仕様テストと同居しています。さらに `GetHashCode()` の非一致を要求する不適切な Assert が複数あります。
- すぐ直すべき点: 誤解を招くテスト名、弱い oracle、white-box テストの隔離、ZDD white-box テストの重複、hash code 非一致 Assert を優先的に直してください。
- 良い点: 固定 seed の randomized test、ZDD naive model、明示 truth table、manager mismatch、enumeration limit、string helper overload のテストは価値があります。v0.7 以降の API 追加に対してテストを足そうとしている姿勢も良いです。

## 2. Review Scope

レビュー対象:

- `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/BddOperatorTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/CoreEdgeTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/DecisionDiagramManagerTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/MtbddManagerTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/ZddManagerTests.cs`
- `tests/DecisionDiagramSharp.Core.Tests/ZmtbddManagerTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/BddDiagnosticsTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/DiagnosticExtensionTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/MtbddDiagnosticsTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/TableModelTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/ZddDiagnosticsTests.cs`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/ZmtbddDiagnosticsTests.cs`
- `tests/DecisionDiagramSharp.Export.Tests/ExportExtensionTests.cs`
- `tests/DecisionDiagramSharp.Export.Tests/TableExportTests.cs`

補助的に確認:

- `tests/DecisionDiagramSharp.Core.Tests/TestHelpers.cs`
- `tests/DecisionDiagramSharp.Core.Tests/ZddNaiveModel.cs`
- 各 `MSTestSettings.cs`

対象外:

- `tests/**/obj/**` 以下の生成ファイル。ビルド生成物であり、レビュー対象の手書き UnitTest ではないため。
- `.csproj`。今回は `[TestClass]`, `[TestMethod]` を持つテストコードの設計レビューが主目的のため。

## 3. Overall Assessment

| 観点 | 点数 | コメント |
|---|---:|---|
| 仕様網羅性 | 7 | BDD/ZDD/MTBDD/ZMTBDD の主要 happy path と境界値は広い。ただし BDD の property 的検証は浅く、例外契約のメッセージ/ParamName は不足。 |
| テスト名の正確性 | 6 | 多くは読めるが、`Ite_WithSameBranches`、`Var_ShouldThrowWhenVariableIdIsNull`、export extension の `AsMarkdown` など実態とズレる名前がある。 |
| Oracle の独立性 | 6 | ZDD naive model と explicit truth table は良い。BDD の algebra identity 系は同じ BDD 演算で期待値を作るものが多く、共通バグに弱い。 |
| 重複の少なさ | 5 | BDD の idempotence/canonical tests、ZDD の argument validation/internal reflection tests、diagnostics/export smoke tests に重複が目立つ。 |
| リファクタリング耐性 | 4 | private field/type/method 名、node id、内部 cache key、`ToString()` の node id 形式に依存するテストが多い。 |
| 保守性 | 5 | `BddManagerTests.cs` が巨大で関心が混在。MTBDD/ZMTBDD も複合テストが多く、失敗時の原因切り分けが難しい。 |
| コメント品質 | 5 | 意図説明は多いが、過剰・重複・実態より強い表現がある。`///Confirms` のようなスペース欠落や文字化けコメントも品質を落としている。 |
| 全体品質 | 6 | 方向性は良いが、coverage 駆動の white-box テストが仕様テストの信頼性を損ねている。整理すればかなり強い suite になる。 |

## 4. Critical Findings

### Finding C-001: White-box coverage tests are mixed into public API specification tests

- Severity: Critical
- File: `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs`, `ZddManagerTests.cs`, `CoreEdgeTests.cs`, `MtbddManagerTests.cs`, `ZmtbddManagerTests.cs`
- Test Method: `InternalValidation_ShouldDetectCorruptedNodeState`, `InternalHelpers_ShouldCoverEdgeCaseBranches`, `InternalValidation_ShouldDetectCorruptedZddNodeState`, `InternalKeyTypes_ShouldImplementValueEquality`, `InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit`, `PrivateKeyTypes_ObjectEqualsAndHashCode_Work`, `SizeLimitAndCorruptedStateValidation_Throw`
- Problem: `BindingFlags.NonPublic`, `GetPrivateField`, `GetNestedType`, `Activator.CreateInstance`, private method invoke によって `_nodes`, `_uniqueTable`, private node/key types, private recursive helper を直接固定しています。
- Why it matters: 内部表現を改善するだけで仕様に関係ないテストが壊れます。BDD/ZDD/MTBDD/ZMTBDD の public behavior を守るテストと、実装不変条件テストの失敗が同じレベルに見えるため、リファクタリングが不当に重くなります。
- Recommended fix: まず削除せず、`BddManagerInternalInvariantTests`, `ZddManagerInternalInvariantTests`, `MtbddManagerInternalInvariantTests`, `ZmtbddManagerInternalInvariantTests` に隔離し、`[TestCategory("WhiteBox")]`, `[TestCategory("InternalInvariant")]`, `[TestCategory("Fragile")]` を付ける。通常の仕様テストと CI gate を分けるか、少なくともレビュー上で区別する。
- Example fix if useful: private recursive helper 直叩きは public `EnumerateModels` / `EnumerateSets` の limit tests に寄せ、到達不能 branch coverage 目的なら `Fragile` に明記する。

### Finding C-002: Tests require different objects to have different hash codes

- Severity: High
- File: `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs:1274`, `ZddManagerTests.cs:386`, `ZddManagerTests.cs:397`, `CoreEdgeTests.cs:309`, `CoreEdgeTests.cs:320`, `MtbddManagerTests.cs:237`, `ZmtbddManagerTests.cs:247`
- Test Method: `InternalKeyTypes_ShouldImplementValueEquality`, `PrivateKeyTypes_ObjectEqualsAndHashCode_Work`
- Problem: `Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode())` のように、異なる値の hash code が異なることを要求しています。
- Why it matters: .NET の hash code contract は「等しい値は同じ hash code」を要求しますが、「等しくない値は異なる hash code」を要求しません。正しい実装でも合法な collision によりテストが失敗します。
- Recommended fix: 非一致 hash assertion は削除する。代わりに「等しい値の hash code が同じ」を確認するか、`Dictionary<TKey, TValue>` に等価キーで入れて lookup できることを確認する。
- Example fix if useful: `Assert.AreEqual(first.GetHashCode(), second.GetHashCode());` と `Assert.IsFalse(first.Equals(third));` に分離する。

### Finding C-003: Several BDD algebra tests use the same implementation as the expected oracle

- Severity: High
- File: `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs:261`, `312`, `336`
- Test Method: `Not_And_ShouldBeEquivalentToOrOfNegatedOperands`, `Implies_ShouldBeEquivalentToNotAOrB`, `Equivalent_ShouldBeEquivalentToNotXor`
- Problem: 期待値を `manager.Or(manager.Not(a), b)` や `manager.Not(manager.Xor(a, b))` で作っています。これは algebra identity の canonicalization smoke test としては意味がありますが、Boolean semantics の独立 oracle ではありません。
- Why it matters: `Not`, `Or`, `Xor`, `Ite` などに共通するバグがある場合、実装同士が同じ間違いをしてテストが通る可能性があります。
- Recommended fix: semantic test は explicit truth table または naive evaluator で検証する。canonical identity test として残す場合は名前を `..._ShouldCanonicalizeTo...` などに変え、目的を明示する。
- Example fix if useful: `Implies_ShouldMatchTruthTable` で 4 assignment を固定期待値 `[true, true, false, true]` と比較する。

### Finding C-004: `Ite_WithSameBranches_ShouldReturnBranch` name is false

- Severity: High
- File: `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs:744`
- Test Method: `Ite_WithSameBranches_ShouldReturnBranch`
- Problem: テスト内容は `Ite(A, True, False) == A` であり、then/else branches は同じではありません。
- Why it matters: テスト名が嘘をつくと、失敗時に原因を誤読します。また本当に必要な `Ite(A, X, X) == X` の reduction test があるかどうかを見誤ります。
- Recommended fix: 現テスト名を `Ite_WithTrueAndFalseBranches_ShouldReturnCondition` に変更する。別途 `Ite_WithIdenticalBranches_ShouldReturnBranch` を追加して `Ite(A, B, B) == B` を確認する。
- Example fix if useful: `var result = manager.Ite(aNode, bNode, bNode); Assert.AreEqual(bNode, result);`

### Finding C-005: ZDD internal tests are duplicated almost verbatim in two classes

- Severity: High
- File: `tests/DecisionDiagramSharp.Core.Tests/ZddManagerTests.cs:332`, `375`, `407`; `tests/DecisionDiagramSharp.Core.Tests/CoreEdgeTests.cs:256`, `299`, `330`
- Test Method: `InternalValidation_ShouldDetectCorruptedZddNodeState`, `InternalKeyTypes_ShouldImplementValueEquality`, `InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit`
- Problem: 同じ private implementation 依存テストが `ZddManagerTests` と `CoreEdgeTests` に重複しています。
- Why it matters: すでに脆い white-box tests が二重化されており、内部変更時の修正コストと失敗ノイズが倍になります。
- Recommended fix: 片方へ統合する。推奨は `ZddManagerInternalInvariantTests.cs` に集約し、`CoreEdgeTests` から ZDD manager の reflection tests を削除または移動する。
- Example fix if useful: `CoreEdgeTests` は `VariableId`, `VariableTable`, shared value objects の public contract に限定する。

### Finding C-006: API behavior is implicitly locked without clearly stating the product decision

- Severity: Medium
- File: `tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs:851`
- Test Method: `EnumerateModels_ShouldExpandVariablesNotUsedInExpression`
- Problem: `EnumerateModels` が式に未使用の登録済み変数まで universe として展開する仕様を固定しています。
- Why it matters: これは重要な API 仕様です。将来「root に到達可能な変数のみを列挙する」設計に変えると破壊的変更になります。現テストはその product decision を十分に目立たせていません。
- Recommended fix: テスト名とコメントに「registered-variable universe」を明示する。README/API docs にも同じ仕様を書く。別途 `CountModels` も同じ universe を使うかをテストする。
- Example fix if useful: `EnumerateModels_UsesRegisteredVariableUniverseAndExpandsDontCares`

### Finding C-007: Composite tests hide failures by testing many contracts at once

- Severity: Medium
- File: `MtbddManagerTests.cs:77`, `ZmtbddManagerTests.cs:84`, `DecisionDiagramManagerTests.cs:15`, diagnostics/export tests
- Test Method: `Handles_NodeViews_Statistics_AndValidation_Work`, `UnifiedManager_ExposesSeparateManagersWithSharedOptions`, `DotNodeValueAndStatisticsTables_AreDeterministic`
- Problem: handles, equality, operators, `ToString`, views, terminal counts, stats, validation が 1 メソッドに詰め込まれています。
- Why it matters: 失敗時に何の仕様が壊れたのか分かりづらく、テスト名も網羅的すぎて specification として弱いです。
- Recommended fix: public API smoke test は残してよいが、重要 contract は `HandleEquality`, `Statistics`, `NodeViews`, `ValidationAcceptsValidDiagram`, `ToString` などに分割する。
- Example fix if useful: `MtbddHandle_ToString_ShouldIncludeNodeId` は必要なら isolated test にし、node id 形式を public API として固定するか判断する。

## 5. Duplicated or Overlapping Tests

| グループ | 該当テスト | 重複内容 | 推奨対応 |
|---|---|---|---|
| BDD idempotence | `And_ShouldBeIdempotent`, `And_SameOperand_ShouldReturnCanonicalOperand` | どちらも `And(A,A) == A`。後者は canonical node identity として書いているが、Assert は同じ。 | 1つに統合するか、semantic test は truth table、canonical test は node identity 目的を名前で明確化。 |
| BDD var canonicalization | `Var_ShouldReturnSatisfiableNonTerminalNode`, `Var_SameVariable_ShouldReturnCanonicalNode`, `Var_StringName_ShouldReturnSameNodeAsVarById` | `Var("A")` / `Var(id)` が同一 canonical node になる確認が重なる。 | public API convenience と canonicalization に分けるならコメントを短く明確に。 |
| BDD Boolean truth table | `BooleanOperations_ShouldMatchExplicitTruthTable`, `BddOperatorTests.Operators_MatchBooleanTruthTableSemantics` | 同じ式 `(A && !B) || (A ^ B)` を manager API と operator API で検証。 | 残す価値あり。operator test は「operator delegates semantics」と明記し、manager test と重複ではなくレイヤ差分として扱う。 |
| BDD implication/equivalence identities | `Implies_ShouldBeEquivalentToNotAOrB`, `Equivalent_ShouldBeEquivalentToNotXor`, randomized op test | algebra identity と semantics が混在。 | identity test と truth-table semantics test に分離。 |
| ZDD argument validation | `ZddManagerTests.ArgumentValidation_ShouldThrowOnNullOrInvalidInputs`, `CoreEdgeTests.ZddManager_ArgumentValidation_ShouldThrowOnNullOrInvalidInputs` | 同じ null/unknown id/MaxSets=0 を検証。 | 片方に統合。CoreEdgeTests から manager-specific validation は外す。 |
| ZDD white-box validation | `ZddManagerTests.InternalValidation...`, `CoreEdgeTests.InternalValidation...` | ほぼ同じ corruption scenarios。 | `ZddManagerInternalInvariantTests` に統合しカテゴリ付与。 |
| ZDD private key tests | `ZddManagerTests.InternalKeyTypes...`, `CoreEdgeTests.InternalKeyTypes...` | private `ZddKey`, `BinaryOpKey` の equality/hash。 | 重複削除。必要なら white-box カテゴリへ。 |
| ZDD private enumeration helper | `ZddManagerTests.InternalEnumerationHelper...`, `CoreEdgeTests.InternalEnumerationHelper...` | `EnumerateSetsRecursive` 直叩き。 | 片方削除または隔離。public limit test で代替可能か検討。 |
| MTBDD/ZMTBDD invalid inputs | `InvalidInputsAndManagerMismatch_ThrowActionableExceptions` in both | 同じ構成で複数例外をまとめて検証。 | 重複ではなく family ごとの contract として残せる。ただしケースを分割し ParamName/message を確認。 |
| Diagnostics/export smoke tests | `DotNodeValueAndStatisticsTables_AreDeterministic`, `Exporters_Format*`, extension tests | タイトルや一部 substring の存在確認が中心。 | smoke test と golden/format contract test を分ける。DOT は golden test 化を検討。 |

## 6. Tests With Misleading Names

| 現在のテスト名 | 問題 | 推奨テスト名 | 補足 |
|---|---|---|---|
| `Ite_WithSameBranches_ShouldReturnBranch` | 実際は `Ite(A, True, False) == A`。branches は同じではない。 | `Ite_WithTrueAndFalseBranches_ShouldReturnCondition` | 別途 `Ite_WithIdenticalBranches_ShouldReturnBranch` を追加。 |
| `Var_ShouldThrowWhenVariableIdIsNull` | 実際は `manager.Var(null!)` の string overload null。`VariableId` は struct で null にならない。 | `Var_StringNameNull_ShouldThrowArgumentNullException` | 重大な名前ズレ。 |
| `MakeSet_StringNames_ShouldReturnEquivalentToVariableIdOverload` | `Assert.AreEqual` は handle equality なので canonical identity を固定している。`Equivalent` だけだと semantic equivalence に見える。 | `MakeSet_StringNames_ShouldReturnSameCanonicalHandleAsVariableIdOverload` | ZDD handle equality の意味を明示。 |
| `MakeFamily_StringNames_ShouldReturnEquivalentToVariableIdOverload` | 上と同じ。 | `MakeFamily_StringNames_ShouldReturnSameCanonicalHandleAsVariableIdOverload` | semantic vs canonical を分ける。 |
| `BddExportExtensions_FormatTruthTableAndModelsAsMarkdown` | Markdown だけでなく CSV/AsciiDoc も検証している。 | `BddExportExtensions_FormatTruthTableAndModelsInAllFormats` | 現名は範囲を過小表現。 |
| `ZddExportExtensions_FormatSetFamiliesAsMarkdown` | Markdown だけでなく CSV/AsciiDoc も検証している。 | `ZddExportExtensions_FormatSetFamiliesInAllFormats` | 同上。 |
| `DotNodeValueAndStatisticsTables_AreDeterministic` | MTBDD/ZMTBDD diagnostics では repeated call の determinism を比較していない。substring と table title/value の smoke test。 | `DotNodeValueAndStatisticsTables_ContainExpectedObservableContent` | `Deterministic` と呼ぶなら 2回生成して完全一致を見る。 |
| `Handles_NodeViews_Statistics_AndValidation_Work` | 何が壊れたか分からない包括名。 | 分割: `HandleEquality_ShouldUseManagerOwnedNodeIdentity`, `Statistics_ShouldMatchReachableViews`, etc. | 複合 smoke test としては許容だが specification 名として弱い。 |
| `Canonicalization_ReducesEqualChildrenAndInternsTerminals` | terminal interning、constant reduction、statistics、terminal value APIs まで検証している。 | 分割推奨 | 名前より Assert 範囲が広すぎる。 |

## 7. White-box / Reflection-based Tests

| テスト名 | 依存している内部要素 | 問題 | 推奨対応 |
|---|---|---|---|
| `BddManagerTests.InternalValidation_ShouldDetectCorruptedNodeState` | `_nodes`, `_uniqueTable`, private `BddNode` | 内部 node storage と unique table 名を固定。 | `InternalInvariantTests` に隔離、`TestCategory("WhiteBox")` と `TestCategory("Fragile")` を付与。 |
| `BddManagerTests.InternalKeyTypes_ShouldImplementValueEquality` | private `BddKey`, `IteKey`, `CountKey` | private cache key 名/constructor を固定。hash 非一致 Assert も不適切。 | 隔離。hash 非一致削除。可能なら public canonicalization/cache observable behavior に置換。 |
| `BddManagerTests.InternalHelpers_ShouldCoverEdgeCaseBranches` | private `CountModelsNode`, `EnumerateModelsRecursive`, `IsReachable`, private `Bdd.NodeId` | private method signature を固定し、coverage のためだけのテストになっている。 | 原則 public API テストに置換。残すなら `Fragile`。 |
| `ZddManagerTests.InternalValidation_ShouldDetectCorruptedZddNodeState` | `_nodes`, `_uniqueTable`, private `ZddNode` | public API 仕様ではなく internal invariant。 | `ZddManagerInternalInvariantTests` に隔離。 |
| `ZddManagerTests.InternalKeyTypes_ShouldImplementValueEquality` | private `ZddKey`, `BinaryOpKey` | private cache structure 固定。hash 非一致 Assert あり。 | 隔離または削除。hash 非一致削除。 |
| `ZddManagerTests.InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit` | private `EnumerateSetsRecursive` | public contract から到達不能な branch を固定。 | public `EnumerateSets` limit test で代替。残すなら `Fragile`。 |
| `CoreEdgeTests.InternalValidation_ShouldDetectCorruptedZddNodeState` | 同上 | `ZddManagerTests` と重複。 | 削除または `ZddManagerInternalInvariantTests` に統合。 |
| `CoreEdgeTests.InternalKeyTypes_ShouldImplementValueEquality` | 同上 | 重複、hash 非一致 Assert。 | 削除または統合。 |
| `CoreEdgeTests.InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit` | 同上 | 重複。 | 削除または統合。 |
| `MtbddManagerTests.SizeLimitAndCorruptedStateValidation_Throw` | `_nodes`, `_uniqueTable`, private `MtbddNode` | size limit public test と corrupted-state white-box test が混在。 | size limit は public error tests、corruption は internal invariant tests に分離。 |
| `MtbddManagerTests.PrivateKeyTypes_ObjectEqualsAndHashCode_Work` | private `MtbddKey` | private key と hash 非一致を固定。 | 隔離。hash 非一致削除。 |
| `MtbddManagerTests.GetNodeId` helper users | private `Mtbdd.NodeId` | `ToString()` や terminal lookup のため private node id を読む。 | public API として node id を固定するなら explicit docs。そうでなければ private access を避ける。 |
| `ZmtbddManagerTests.SizeLimitAndCorruptedStateValidation_Throw` | `_nodes`, `_uniqueTable`, private `ZmtbddNode` | public and white-box 混在。 | 分離してカテゴリ付与。 |
| `ZmtbddManagerTests.PrivateKeyTypes_ObjectEqualsAndHashCode_Work` | private `ZmtbddKey` | private key と hash 非一致を固定。 | 隔離。hash 非一致削除。 |
| `ZmtbddManagerTests.GetNodeId` helper users | private `Zmtbdd.NodeId` | handle internals を固定。 | public behavior で置換、または public contract 化を判断。 |

## 8. Oracle Improvement Suggestions

| テスト名 | 現在の期待値の作り方 | 問題 | 推奨 oracle |
|---|---|---|---|
| `Not_And_ShouldBeEquivalentToOrOfNegatedOperands` | `manager.Or(manager.Not(a), manager.Not(b))` | 同じ BDD 演算群に依存。 | explicit truth table for `!(A && B)`、または expression tree naive evaluator。 |
| `Not_Or_ShouldBeEquivalentToAndOfNegatedOperands` | `manager.And(manager.Not(a), manager.Not(b))` | 同上。 | explicit truth table。canonical identity test として残すなら名前変更。 |
| `Implies_ShouldBeEquivalentToNotAOrB` | `manager.Or(manager.Not(a), b)` | `Implies`, `Or`, `Not` の共通バグを検出しづらい。 | 4-row truth table `[true, true, false, true]`。 |
| `Equivalent_ShouldBeEquivalentToNotXor` | `manager.Not(manager.Xor(a,b))` | `Equivalent`, `Xor`, `Not` の共通バグに弱い。 | 4-row truth table `[true, false, false, true]`。 |
| `Exists_Over*`, `ForAll_Over*` | 期待値に `manager.Var(b)` や terminals を使う | 小例としては良いが、quantification の一般性は弱い。 | naive truth-table quantifier: quantified variable を全 assignment で OR/AND。 |
| `RandomizedOperations_ShouldMatchNaiveTruthTables` | left/right variable 2個だけの単発 operation | 固定 seed は良いが、式木ではないため apply/cache/order の複雑な相互作用を拾いにくい。 | randomized expression tree + independent evaluator + all assignments。 |
| `Handles_NodeViews_Statistics_AndValidation_Work` | `manager.Create(values)` を再度呼び `Assert.AreEqual` | canonicalization smoke にはなるが semantic oracle ではない。 | semantic は truth table、canonical は node count/view invariants として分離。 |
| Diagnostics DOT tests | substring contains | DOT 全体の安定性・edge correctness を保証しきれない。 | normalized golden string or focused graph model assertions。 |
| Export extension tests | substring contains | format regression に弱い。 | small table golden output。extension tests は delegation smoke に限定。 |

## 9. Proposed Test Class Restructuring

BDD:

```text
BddManagerTerminalTests.cs
BddManagerVariableTests.cs
BddManagerBooleanOperationTests.cs
BddManagerIteTests.cs
BddManagerRestrictionTests.cs
BddManagerQuantificationTests.cs
BddManagerModelEnumerationTests.cs
BddManagerCanonicalizationTests.cs
BddManagerErrorContractTests.cs
BddManagerStringApiTests.cs
BddManagerRandomizedPropertyTests.cs
BddManagerInternalInvariantTests.cs
BddOperatorTests.cs
```

ZDD:

```text
ZddManagerTerminalTests.cs
ZddManagerConstructionTests.cs
ZddManagerSetOperationTests.cs
ZddManagerSubsetOperationTests.cs
ZddManagerEnumerationTests.cs
ZddManagerCanonicalizationTests.cs
ZddManagerErrorContractTests.cs
ZddManagerStringApiTests.cs
ZddManagerRandomizedNaiveModelTests.cs
ZddManagerInternalInvariantTests.cs
```

MTBDD / ZMTBDD:

```text
MtbddManagerConstructionEvaluationTests.cs
MtbddManagerCanonicalizationTests.cs
MtbddManagerStatisticsAndViewsTests.cs
MtbddManagerErrorContractTests.cs
MtbddManagerRandomizedTruthTableTests.cs
MtbddManagerInternalInvariantTests.cs

ZmtbddManagerConstructionEvaluationTests.cs
ZmtbddManagerZeroSuppressionTests.cs
ZmtbddManagerStatisticsAndViewsTests.cs
ZmtbddManagerErrorContractTests.cs
ZmtbddManagerRandomizedTruthTableTests.cs
ZmtbddManagerInternalInvariantTests.cs
```

Shared / facade:

```text
VariableIdTests.cs
VariableTableTests.cs
DecisionDiagramManagerFacadeTests.cs
CorePublicValueObjectTests.cs
```

Diagnostics / Export:

```text
BddDiagnosticsDotTests.cs
BddDiagnosticsTableTests.cs
ZddDiagnosticsDotTests.cs
ZddDiagnosticsTableTests.cs
MtbddDiagnosticsTests.cs
ZmtbddDiagnosticsTests.cs
TableExporterGoldenTests.cs
ExportExtensionSmokeTests.cs
```

## 10. Prioritized Action Plan

### Phase 1: Must fix before expanding tests

* Rename clearly misleading tests: `Ite_WithSameBranches_ShouldReturnBranch`, `Var_ShouldThrowWhenVariableIdIsNull`, export extension `AsMarkdown`, diagnostics `AreDeterministic`.
* Remove all `Assert.AreNotEqual(...GetHashCode())` assertions.
* Move reflection/private implementation tests into `*InternalInvariantTests` classes and add `TestCategory("WhiteBox")`, `TestCategory("InternalInvariant")`, `TestCategory("Fragile")`.
* Deduplicate ZDD white-box tests currently repeated in `ZddManagerTests` and `CoreEdgeTests`.
* Keep existing tests passing after only mechanical moves/renames.

### Phase 2: Improve test design

* Convert BDD algebra identity tests into two layers: semantic truth-table tests and canonical identity tests.
* Introduce a small independent BDD expression-tree evaluator for randomized/property-like tests.
* Split `BddManagerTests.cs` into purpose-specific classes.
* Split MTBDD/ZMTBDD composite tests into construction/evaluation, statistics/views, handle equality, error contract, and internal invariant tests.
* Add exception `ParamName` and selected actionable message assertions where the API contract promises caller guidance.

### Phase 3: Strengthen quality gate

* Add randomized BDD expression tree tests with fixed seed, printed iteration/tree/mask on failure, and all truth assignments.
* Expand MTBDD/ZMTBDD randomized tests beyond 3 variables where bounded and cheap, with naive integer truth-table oracle.
* Convert diagnostics/export output tests that claim stability into golden tests.
* Decide CI handling for `Fragile` white-box tests: run by default but separately reported, or run in a dedicated internal-invariant job.
* Document API universe choices such as `EnumerateModels` using all registered variables.

## 11. Concrete Rewrite Prompts

```text
v0.7.1

目的:
BDD/ZDD/MTBDD/ZMTBDD テスト内の明らかに不適切な Assert と嘘をつくテスト名を修正する。

対象:
tests/DecisionDiagramSharp.Core.Tests 配下の既存テスト。

制約:
production code は変更しない。テストの意図が不明なものは削除しない。挙動は変えず rename と Assert 修正を優先する。

実施内容:
GetHashCode() の非一致 Assert を削除または等価値の hash 一致確認へ置換する。
Ite_WithSameBranches_ShouldReturnBranch を実態に合う名前へ変更し、必要なら Ite(A, X, X) の別テストを追加する。
Var_ShouldThrowWhenVariableIdIsNull を string-name null のテスト名へ変更する。
Export/Diagnostics の Markdown/Deterministic など実態とズレる名前を修正する。

完了条件:
dotnet test が通る。
変更した各テスト名が Arrange/Act/Assert と一致している。
```

```text
v0.7.2

目的:
private reflection に依存する white-box テストを通常の public API 仕様テストから隔離する。

対象:
BddManagerTests.cs, ZddManagerTests.cs, CoreEdgeTests.cs, MtbddManagerTests.cs, ZmtbddManagerTests.cs。

制約:
reflection テストは削除しない。まず移動と分類だけを行う。production code は変更しない。

実施内容:
BddManagerInternalInvariantTests.cs, ZddManagerInternalInvariantTests.cs, MtbddManagerInternalInvariantTests.cs, ZmtbddManagerInternalInvariantTests.cs を作成する。
BindingFlags.NonPublic, GetPrivateField, Activator.CreateInstance, private method Invoke を使うテストを移動する。
各クラスまたは各メソッドに TestCategory("WhiteBox"), TestCategory("InternalInvariant"), TestCategory("Fragile") を付ける。
ZDD の重複 white-box テストは 1 箇所に統合する。

完了条件:
dotnet test が通る。
通常の manager tests から private reflection usage が消えている。
```

```text
v0.7.3

目的:
BDD の Boolean algebra tests の oracle を強化し、semantic test と canonicalization test を分離する。

対象:
BddManagerTests.cs から分割される BDD Boolean operation / canonicalization tests。

制約:
既存の public behavior を勝手に変えない。canonical node identity を固定する価値があるテストは削除せず、名前とコメントで目的を明示する。

実施内容:
Implies, Equivalent, De Morgan, Xor, Ite の truth-table based tests を追加または置換する。
同じ BDD manager operation で期待値を作るテストは canonical identity tests としてリネームする。
Assert.AreEqual が semantic equality なのか canonical handle equality なのかメッセージで明示する。

完了条件:
dotnet test が通る。
主要 BDD operations が explicit truth table または naive evaluator で検証されている。
```

```text
v0.7.4

目的:
BddManagerTests.cs の巨大化を解消し、目的別クラスに分割して保守性を上げる。

対象:
tests/DecisionDiagramSharp.Core.Tests/BddManagerTests.cs。

制約:
一度に production code は変更しない。テスト内容の削除は重複が明確なものだけにする。移動後も履歴上の意図が分かる名前を保つ。

実施内容:
Terminal, Variable, BooleanOperation, Ite, Restriction, Quantification, ModelEnumeration, Canonicalization, ErrorContract, StringApi, RandomizedProperty に分割する。
Arrange/Act/Assert コメントを整える。
過剰な XML remarks を短くし、目的・重要性・防ぐバグに限定する。

完了条件:
dotnet test が通る。
各新規クラスが単一のテスト目的を持つ。
BddManagerTests.cs が空または削除されている。
```

```text
v0.7.5

目的:
randomized/property-like tests を強化し、BDD/MTBDD/ZMTBDD のバグ検出力を上げる。

対象:
BDD randomized tests、MTBDD/ZMTBDD randomized truth-table tests、ZDD naive model tests。

制約:
seed は固定する。失敗時に seed, iteration, expression/values, assignment mask が分かるメッセージを出す。実行時間を unit test として現実的に保つ。

実施内容:
BDD はランダム式木を生成し、独立 expression-tree evaluator と全 assignment で比較する。
MTBDD/ZMTBDD は variable count と value distribution を数パターンに増やす。
ZDD は既存 naive model test を維持しつつ、空 family/base/singleton の edge cases を randomized loop に混ぜる。

完了条件:
dotnet test が通る。
randomized tests が単発演算だけでなく複合式・複合構造を検証している。
```
