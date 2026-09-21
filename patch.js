
const fs = require("fs");

let ctrlPath = "c:/AntiGravity Proyectos/GestionQ/src/GestionQ.Web/Controllers/PaymentReceiptsController.cs";
let ctrlContent = fs.readFileSync(ctrlPath, "utf8");
ctrlContent = ctrlContent.replace(/!p.IsCancelled/g, "p.Status != PurchaseStatus.Cancelled");
ctrlContent = ctrlContent.replace(/p.InvoiceNumber/g, "p.ReferenceNumber");
fs.writeFileSync(ctrlPath, ctrlContent, "utf8");

let printPath = "c:/AntiGravity Proyectos/GestionQ/src/GestionQ.Web/Views/PaymentReceipts/Print.cshtml";
let printContent = fs.readFileSync(printPath, "utf8");
printContent = printContent.replace(/Model.Purchase.InvoiceNumber/g, "Model.Purchase.ReferenceNumber");
fs.writeFileSync(printPath, printContent, "utf8");

console.log("Done");

