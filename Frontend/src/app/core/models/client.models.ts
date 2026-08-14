export interface ClientSummary {
  id: string;
  name: string;
  contactName: string;
  contactEmail: string;
  phoneNumber: string;
  projects: number;
  openTasks: number;
}

export interface ClientDetails extends ClientSummary {
  description: string;
}

export interface ClientUpsertRequest {
  name: string;
  contactName: string;
  contactEmail: string;
  phoneNumber: string;
  description: string;
}