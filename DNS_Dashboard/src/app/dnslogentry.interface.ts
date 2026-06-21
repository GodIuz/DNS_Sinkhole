export interface DnsLogEntry {
  timestamp: string;
  domain: string;
  clientIp: string;
  isBlocked: boolean;
}