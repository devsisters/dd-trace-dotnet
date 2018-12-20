#include "integration_loader.h"

#include <exception>
#include <stdexcept>

#include "logging.h"
#include "util.h"
#include "environment_variables.h"

namespace trace {

using json = nlohmann::json;

std::vector<Integration> LoadIntegrationsFromEnvironment() {
  std::vector<Integration> integrations;
  for (const auto f : GetEnvironmentValues(environment::integrations_path)) {
    Info("Loading integrations from file: ", f);
    auto is = LoadIntegrationsFromFile(f);
    for (auto& i : is) {
      integrations.push_back(i);
    }
  }
  return integrations;
}

std::vector<Integration> LoadIntegrationsFromFile(const WSTRING& file_path) {
  std::vector<Integration> integrations;

  try {
    std::ifstream stream;
    stream.open(ToString(file_path));
    integrations = LoadIntegrationsFromStream(stream);
    stream.close();
  } catch (...) {
    auto ex = std::current_exception();
    try {
      if (ex) {
        std::rethrow_exception(ex);
      }
    } catch (const std::exception& ex) {
      Warn("Failed to load integrations: ", ex.what());
    }
  }

  return integrations;
}

std::vector<Integration> LoadIntegrationsFromStream(std::istream& stream) {
  std::vector<Integration> integrations;

  try {
    json j;
    // parse the stream
    stream >> j;

    for (auto& el : j) {
      auto i = IntegrationFromJson(el);
      if (std::get<1>(i)) {
        integrations.push_back(std::get<0>(i));
      }
    }

    Info("Loaded integrations: ", j.dump());
  } catch (const json::parse_error& e) {
    Warn("Invalid integrations:", e.what());
  } catch (const json::type_error& e) {
    Warn("Invalid integrations:", e.what());
  } catch (...) {
    auto ex = std::current_exception();
    try {
      if (ex) {
        std::rethrow_exception(ex);
      }
    } catch (const std::exception& ex) {
      Warn("Failed to load integrations: ", ex.what());
    }
  }

  return integrations;
}

namespace {

std::pair<Integration, bool> IntegrationFromJson(const json::value_type& src) {
  if (!src.is_object()) {
    return std::make_pair<Integration, bool>({}, false);
  }

  // first get the name, which is required
  auto name = ToWSTRING(src.value("name", ""));
  if (name.empty()) {
    Warn("Integration name is missing for integration: ", src.dump());
    return std::make_pair<Integration, bool>({}, false);
  }

  std::vector<MethodReplacement> replacements;
  auto arr = src.value("method_replacements", json::array());
  if (arr.is_array()) {
    for (auto& el : arr) {
      auto mr = MethodReplacementFromJson(el);
      if (std::get<1>(mr)) {
        replacements.push_back(std::get<0>(mr));
      }
    }
  }
  return std::make_pair<Integration, bool>({name, replacements}, true);
}

std::pair<MethodReplacement, bool> MethodReplacementFromJson(
    const json::value_type& src) {
  if (!src.is_object()) {
    return std::make_pair<MethodReplacement, bool>({}, false);
  }

  const auto caller = MethodReferenceFromJson(src.value("caller", json::object()));
  const auto target = MethodReferenceFromJson(src.value("target", json::object()));
  const auto wrapper = MethodReferenceFromJson(src.value("wrapper", json::object()));
  return std::make_pair<MethodReplacement, bool>({caller, target, wrapper},
                                                 true);
}

MethodReference MethodReferenceFromJson(const json::value_type& src) {
  if (!src.is_object()) {
    return {};
  }

  const auto assembly = ToWSTRING(src.value("assembly", ""));
  const auto type = ToWSTRING(src.value("type", ""));
  const auto method = ToWSTRING(src.value("method", ""));
  auto raw_signature = src.value("signature", json::array());
  std::vector<BYTE> signature;
  if (raw_signature.is_array()) {
    for (auto& el : raw_signature) {
      if (el.is_number_unsigned()) {
        signature.push_back(BYTE(el.get<BYTE>()));
      }
    }
  } else if (raw_signature.is_string()) {
    // load as a hex string
    std::string str = raw_signature;
    bool flip = false;
    char prev = 0;
    for (auto& c : str) {
      BYTE b = 0;
      if ('0' <= c && c <= '9') {
        b = c - '0';
      } else if ('a' <= c && c <= 'f') {
        b = c - 'a' + 10;
      } else if ('A' <= c && c <= 'F') {
        b = c - 'A' + 10;
      } else {
        // skip any non-hex character
        continue;
      }
      if (flip) {
        signature.push_back((prev << 4) + b);
      }
      flip = !flip;
      prev = b;
    }
  }
  return MethodReference(assembly, type, method, signature);
}

}  // namespace

}  // namespace trace
