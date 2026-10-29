//! Blocking native transport smoke gate. It intentionally has no mock path: a
//! missing local Agent or an anonymously reachable health endpoint is failure.
use std::io::{Read, Write};
use std::net::TcpStream;

fn main() {
    let mut stream = TcpStream::connect("127.0.0.1:5080")
        .expect("[BLOCKING] local Helix Agent is not listening on IPv4 loopback");
    stream
        .write_all(b"GET /health HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n")
        .expect("[BLOCKING] unable to write native Agent probe");
    let mut response = String::new();
    stream.read_to_string(&mut response).expect("[BLOCKING] unable to read native Agent probe");
    assert!(response.starts_with("HTTP/1.1 401") || response.starts_with("HTTP/1.1 403"),
        "[BLOCKING] Agent health must require an enrolled desktop session");
    println!("native broker smoke passed: unauthenticated loopback health was denied");
}
