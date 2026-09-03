"use client";

import { useState } from "react";
import { Menu, X } from "lucide-react";
import LogoutButton from "./LogoutButton";

export default function DashboardNavbar({
  user,
}: {
  user: {
    name?: string | null;
    role?: string | null;
  };
}) {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  return (
    <nav className="bg-slate-900 shadow-lg border-b border-slate-800">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 justify-between">
          <div className="flex">
            <div className="flex flex-shrink-0 items-center">
              <div className="h-8 w-8 bg-gradient-to-tr from-blue-500 to-indigo-500 rounded-lg flex items-center justify-center mr-3 shadow-md">
                <span className="text-white font-bold text-lg">Q</span>
              </div>
              <span className="text-xl font-bold text-white tracking-wide">
                Gestion<span className="text-blue-400">Q</span>
              </span>
            </div>
            {/* Desktop Navigation */}
            <div className="hidden md:ml-10 md:flex md:items-center md:space-x-2">
              <a
                href="/dashboard"
                className="text-slate-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                Dashboard
              </a>
              {user.role === "ADMIN" && (
                <a
                  href="/dashboard/admin"
                  className="text-slate-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium transition-colors flex items-center gap-2"
                >
                  <span className="w-2 h-2 rounded-full bg-red-500"></span>
                  Administración SaaS
                </a>
              )}
            </div>
          </div>
          {/* Desktop User Info & Actions */}
          <div className="hidden md:flex md:items-center">
            <div className="flex items-center gap-3">
              <div className="flex flex-col text-right">
                <span className="text-sm font-medium text-white">
                  {user.name}
                </span>
                <span className="text-xs text-slate-400">
                  {user.role === "ADMIN" ? "Administrador" : "Cliente"}
                </span>
              </div>
              <div className="h-9 w-9 rounded-full bg-slate-700 flex items-center justify-center border border-slate-600 text-white font-semibold">
                {user.name?.charAt(0) || "U"}
              </div>
              <LogoutButton />
            </div>
          </div>

          {/* Mobile menu button */}
          <div className="flex items-center md:hidden">
            <button
              type="button"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="inline-flex items-center justify-center p-2 rounded-md text-slate-400 hover:text-white hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-blue-500"
              aria-expanded="false"
            >
              <span className="sr-only">Abrir menú principal</span>
              {isMobileMenuOpen ? (
                <X className="block h-6 w-6" aria-hidden="true" />
              ) : (
                <Menu className="block h-6 w-6" aria-hidden="true" />
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile Menu */}
      {isMobileMenuOpen && (
        <div className="md:hidden border-t border-slate-800">
          <div className="px-2 pt-2 pb-3 space-y-1 sm:px-3">
            <a
              href="/dashboard"
              className="text-slate-300 hover:text-white hover:bg-slate-800 block px-3 py-2 rounded-md text-base font-medium"
            >
              Dashboard
            </a>
            {user.role === "ADMIN" && (
              <a
                href="/dashboard/admin"
                className="text-slate-300 hover:text-white hover:bg-slate-800 flex items-center gap-2 px-3 py-2 rounded-md text-base font-medium"
              >
                <span className="w-2 h-2 rounded-full bg-red-500"></span>
                Administración SaaS
              </a>
            )}
          </div>
          <div className="pt-4 pb-3 border-t border-slate-800">
            <div className="flex items-center px-5 gap-3">
              <div className="h-10 w-10 rounded-full bg-slate-700 flex items-center justify-center border border-slate-600 text-white font-semibold flex-shrink-0">
                {user.name?.charAt(0) || "U"}
              </div>
              <div className="flex-col text-left flex-1">
                <div className="text-base font-medium text-white">
                  {user.name}
                </div>
                <div className="text-sm text-slate-400">
                  {user.role === "ADMIN" ? "Administrador" : "Cliente"}
                </div>
              </div>
              <LogoutButton />
            </div>
          </div>
        </div>
      )}
    </nav>
  );
}
