"use client";

import { useState, useEffect } from "react";
import { Plus, Key, Copy, Check } from "lucide-react";

export default function AdminPanelClient() {
  const [tenants, setTenants] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [formLoading, setFormLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [copiedKey, setCopiedKey] = useState("");

  const [formData, setFormData] = useState({
    companyName: "",
    email: "",
    password: ""
  });

  useEffect(() => {
    fetchTenants();
  }, []);

  async function fetchTenants() {
    try {
      const res = await fetch("/api/admin/tenants");
      if (res.ok) {
        const data = await res.json();
        setTenants(data);
      }
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormLoading(true);
    try {
      const res = await fetch("/api/admin/tenants", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData)
      });
      if (res.ok) {
        setShowForm(false);
        setFormData({ companyName: "", email: "", password: "" });
        fetchTenants();
      } else {
        const data = await res.json();
        alert(data.error);
      }
    } catch (error) {
      alert("Error de conexión");
    } finally {
      setFormLoading(false);
    }
  }

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(text);
    setTimeout(() => setCopiedKey(""), 2000);
  };

  if (loading) return <div>Cargando empresas...</div>;

  return (
    <div className="space-y-6">
      {/* Botón Nueva Empresa */}
      <div className="flex justify-end">
        <button
          onClick={() => setShowForm(!showForm)}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700"
        >
          <Plus className="-ml-1 mr-2 h-5 w-5" />
          Nuevo Cliente
        </button>
      </div>

      {/* Formulario Nueva Empresa */}
      {showForm && (
        <div className="bg-white shadow sm:rounded-lg">
          <div className="px-4 py-5 sm:p-6">
            <h3 className="text-lg leading-6 font-medium text-gray-900">Agregar Nuevo Cliente</h3>
            <form onSubmit={handleSubmit} className="mt-5 space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Nombre de la Empresa</label>
                  <input
                    required
                    type="text"
                    value={formData.companyName}
                    onChange={(e) => setFormData({...formData, companyName: e.target.value})}
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Email (Acceso Dashboard)</label>
                  <input
                    required
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({...formData, email: e.target.value})}
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Contraseña temporal</label>
                  <input
                    required
                    type="text"
                    value={formData.password}
                    onChange={(e) => setFormData({...formData, password: e.target.value})}
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  />
                </div>
              </div>
              <div className="flex justify-end">
                <button
                  type="submit"
                  disabled={formLoading}
                  className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
                >
                  {formLoading ? "Creando..." : "Crear Cliente y Generar API Key"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Lista de Empresas */}
      <div className="bg-white shadow sm:rounded-lg overflow-hidden">
        <ul className="divide-y divide-gray-200">
          {tenants.map((tenant) => (
            <li key={tenant.id} className="px-4 py-4 sm:px-6 hover:bg-gray-50">
              <div className="flex items-center justify-between">
                <div className="flex flex-col">
                  <p className="text-sm font-medium text-blue-600 truncate">{tenant.companyName}</p>
                  <p className="text-sm text-gray-500 mt-1">Usuario: {tenant.users[0]?.email}</p>
                  <p className="text-xs text-gray-400 mt-1">Creado: {new Date(tenant.createdAt).toLocaleDateString()}</p>
                </div>
                
                <div className="flex items-center bg-gray-100 px-3 py-2 rounded-md">
                  <Key className="h-4 w-4 text-gray-500 mr-2" />
                  <span className="text-xs font-mono text-gray-600 mr-3 truncate w-48" title={tenant.apiKey}>
                    {tenant.apiKey}
                  </span>
                  <button
                    onClick={() => copyToClipboard(tenant.apiKey)}
                    className="text-gray-500 hover:text-blue-600 focus:outline-none"
                    title="Copiar API Key"
                  >
                    {copiedKey === tenant.apiKey ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </button>
                </div>
              </div>
            </li>
          ))}
          {tenants.length === 0 && (
            <li className="px-4 py-8 text-center text-gray-500">
              Aún no hay clientes registrados.
            </li>
          )}
        </ul>
      </div>
    </div>
  );
}
