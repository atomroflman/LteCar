import React, { useState } from "react";

interface CollapsibleSectionProps {
  title: string;
  children: React.ReactNode;
  defaultCollapsed?: boolean;
  className?: string;
}

export default function CollapsibleSection({ 
  title, 
  children, 
  defaultCollapsed = false,
  className = ""
}: CollapsibleSectionProps) {
  const [collapsed, setCollapsed] = useState(defaultCollapsed);

  return (
    <div className={collapsed 
      ? "mt-2 bg-zinc-900 text-zinc-100 rounded-lg p-0 text-xs leading-tight" 
      : `mt-2 bg-zinc-900 text-zinc-100 rounded-lg border border-zinc-800 text-xs leading-tight ${className}`
    }>
      <button
        className="mb-2 px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 rounded text-xs border border-zinc-700 transition-colors duration-150 w-full flex items-center justify-between"
        onClick={() => setCollapsed(c => !c)}
        aria-label={collapsed ? `Expand ${title}` : `Collapse ${title}`}
        style={collapsed ? { marginBottom: 0 } : {}}
      >
        <span>{title}</span>
        <span className="ml-2">{collapsed ? '▼' : '▲'}</span>
      </button>
      {!collapsed && (
        <div className="px-2 pb-2">
          {children}
        </div>
      )}
    </div>
  );
}
