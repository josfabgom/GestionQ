import { getServerSession } from "next-auth/next";
import { authOptions } from "../api/auth/[...nextauth]/route";
import DashboardMetrics from "./DashboardMetrics";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

export default async function DashboardPage() {
  const session = await getServerSession(authOptions);

  if (!session) {
    return <div>No autorizado</div>;
  }

  // Leer stats directamente desde la base de datos central de la VPS
  const tenant = await prisma.tenant.findUnique({
    where: { id: session.user.tenantId! }
  });

  const rawStats = tenant?.dashboardStats ? JSON.parse(tenant.dashboardStats) : null;

  return (
    <div className="space-y-8">
      {/* Header moderno */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-slate-900">
            Resumen de Negocio
          </h2>
          <p className="mt-2 text-sm text-slate-500">
            Vista general de <span className="font-semibold text-slate-700">{tenant?.companyName}</span>
          </p>
        </div>
        
        {tenant?.lastSyncAt && (
          <div className="flex items-center gap-2 bg-white px-3 py-1.5 rounded-full shadow-sm border border-slate-200">
            <span className="relative flex h-2.5 w-2.5">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500"></span>
            </span>
            <span className="text-xs font-medium text-slate-600">
              Sincronizado: {tenant.lastSyncAt.toLocaleString("es-AR", { hour: '2-digit', minute: '2-digit' })}
            </span>
          </div>
        )}
      </div>
      
      {/* Pasar los datos sincronizados */}
      <DashboardMetrics initialData={rawStats} />
    </div>
  );
}

// Simple server wrapper, in a real app this would be a separate 'use client' file.
// Wait, I will just create a Client Component below or separately.
