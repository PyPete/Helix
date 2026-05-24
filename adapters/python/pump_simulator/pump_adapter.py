#!/usr/bin/env python3
import grpc
from concurrent import futures
import json
import time
import logging
from generated import adapter_pb2, adapter_pb2_grpc


class PumpSimulatorAdapter(adapter_pb2_grpc.DeviceAdapterServicer):
    def __init__(self):
        self.flow_rate = 0.0
        self.is_running = False
        self.capabilities = ["SetFlowRate", "GetFlowRate", "Start", "Stop"]

    def GetCapabilities(self, request, context):
        return adapter_pb2.CapabilitiesResponse(capabilities=self.capabilities)

    def InvokeCapability(self, request, context):
        cap_name = request.capability_name
        params = json.loads(request.parameters_json) if request.parameters_json else {}

        if cap_name == "SetFlowRate":
            self.flow_rate = params.get("flowRate", 0.0)
            return adapter_pb2.CapabilityResponse(
                success=True,
                result_json=json.dumps({"flowRate": self.flow_rate})
            )
        if cap_name == "Start":
            self.is_running = True
            return adapter_pb2.CapabilityResponse(success=True)
        if cap_name == "Stop":
            self.is_running = False
            return adapter_pb2.CapabilityResponse(success=True)
        if cap_name == "GetFlowRate":
            return adapter_pb2.CapabilityResponse(
                success=True,
                result_json=json.dumps({"flowRate": self.flow_rate, "running": self.is_running})
            )

        return adapter_pb2.CapabilityResponse(
            success=False,
            error_message=f"Unknown capability: {cap_name}"
        )

    def StreamTrends(self, request, context):
        while True:
            if self.is_running:
                yield adapter_pb2.TrendValue(
                    trend_id="pump-01-flow",
                    trend_kind="Scalar",
                    value_json=json.dumps({"value": self.flow_rate, "units": "mL/min"}),
                    timestamp=int(time.time() * 1000)
                )
            time.sleep(1.0)

    def HealthCheck(self, request, context):
        return adapter_pb2.HealthStatus(healthy=True, message="Pump simulator online")


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    adapter_pb2_grpc.add_DeviceAdapterServicer_to_server(PumpSimulatorAdapter(), server)
    server.add_insecure_port("[::]:50051")
    logging.info("Pump Simulator Adapter listening on port 50051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    serve()
