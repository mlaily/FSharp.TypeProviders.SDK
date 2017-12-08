
namespace ProviderImplementation

open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open FSharp.Quotations
open FSharp.Core.CompilerServices
open System.Reflection

[<TypeProvider>]
type BasicErasingProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, [("BasicProvider.DesignTime", "BasicProvider")])

    let ns = "BasicProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<BasicProvider.Helpers.SomeRuntimeHelper>.Assembly.GetName().Name = asm.GetName().Name)  

    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[0]):string) :> obj @@>)
        myType.AddMember(ctor2)

        let innerState = ProvidedProperty("InnerState", typeof<string>, getterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        myType.AddMember(innerState)

        let meth = ProvidedMethod("StaticMethod", [], typeof<BasicProvider.Helpers.SomeRuntimeHelper>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<BasicProvider.Helpers.SomeRuntimeHelper>)))
        myType.AddMember(meth)

        let nameOf =
            let param = ProvidedParameter("p", typeof<Microsoft.FSharp.Quotations.Expr<int>>)
            param.AddCustomAttribute {
                new CustomAttributeData() with
                    member __.Constructor = typeof<ReflectedDefinitionAttribute>.GetConstructor([||])
                    member __.ConstructorArguments = [||] :> _
                    member __.NamedArguments = [||] :> _
            }
            ProvidedMethod("NameOf", [ param ], typeof<string>, isStatic = true, invokeCode = fun args ->
                <@@
                    match (%%args.[0]) : Microsoft.FSharp.Quotations.Expr<int> with
                    | Microsoft.FSharp.Quotations.Patterns.ValueWithName (_, _, n) -> n
                    | e -> failwithf "Invalid quotation argument (expected ValueWithName): %A" e
                @@>)
        myType.AddMember(nameOf)

        [myType]

    do
        this.AddNamespace(ns, createTypes())

