namespace EvReact.Test

open EvReact
open NUnit.Framework

module Utils =
  let serializationClone<'T> (x:'T) =
    use stream = new System.IO.MemoryStream()
    let formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
    formatter.Serialize(stream, x)
    stream.Flush()
    stream.Position <- 0L
    formatter.Deserialize(stream) :?> 'T

  let prepare patterns =
    let events = new System.Collections.Generic.Dictionary<_,_>()
    for pattern in patterns do
      for v in pattern do
        if System.Char.IsLetter(v) && not (events.ContainsKey(v)) then
          events.[v] <- Event.create(string v)

    let check expr =
      for pattern in patterns do
        let matchCount = ref 0
        let receivedArgs = ref null
        let update args =
          matchCount := !matchCount + 1
          receivedArgs := args

        let orch = Orchestrator.createDebug()
        let mutable expectedMatchCount = 0
        let mutable expectedArgs = null
        let mutable sentArgs = obj()
        let mutable expectedEmpty = false
        let mutable net = expr |> Expr.react update |> Expr.start sentArgs orch

        let find v = Seq.exists ((=) v)
        let mutable prefix = sprintf "%A <- " net

        for v in pattern do
          prefix <- prefix + string(v)

          // 0 indicates net termination, with no match
          // . indicates match without net termination
          // ! indicates match and net termination together
          // letters indicate event triggers

          if find v "0!" then
            expectedEmpty <- true

          if find v ".!" then
              expectedArgs <- sentArgs
              expectedMatchCount <- expectedMatchCount + 1

          Assert.AreEqual(expectedEmpty, orch.IsEmpty,
                          "{0}\n{1} Bad state", prefix, sprintf "%A" net)
          Assert.AreEqual(expectedMatchCount, !matchCount,
                          "{0}\n{1} Bad match count", prefix, sprintf "%A" net)
          Assert.AreEqual(expectedArgs, !receivedArgs,
                          "{0}\n{1} Bad arguments", prefix, sprintf "%A" net)

          if not (find v "0.!") then
            sentArgs <- obj()
            events.[v].Trigger(sentArgs)

        Expr.stop net
        Assert.IsTrue(orch.IsEmpty,
                      "{0}\n{1} Could not stop net", prefix, sprintf "%A" net)

    let ievents = new System.Collections.Generic.Dictionary<_,_>()
    for e in events do
      ievents.[e.Key] <- e.Value.Publish
    let getevent v = ievents.[v]
    getevent,check
