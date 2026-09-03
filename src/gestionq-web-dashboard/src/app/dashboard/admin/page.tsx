import { getServerSession } from "next-auth/next";
import { authOptions } from "../../api/auth/[...nextauth]/route";
import { redirect } from "next/navigation";
import AdminPanelClient from "./AdminPanelClient";

export default async function AdminPage() {
  const session = await getServerSession(authOptions);

  if (!session || session.user.role !== "ADMIN") {
    redirect("/dashboard");
  }

  return (
    <div className="space-y-6">
      <div className="bg-white px-4 py-5 shadow sm:rounded-lg sm:p-6">
        <h3 className="text-lg font-medium leading-6 text-gray-900">
          Panel de Administración SaaS
        </h3>
        <p className="mt-1 text-sm text-gray-500">
          Gestiona las empresas cliente y sus accesos al dashboard.
        </p>
      </div>

      <AdminPanelClient />
    </div>
  );
}
